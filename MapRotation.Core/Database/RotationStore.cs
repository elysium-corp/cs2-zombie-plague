using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using MapRotation.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MapRotation.Core.Database;

internal sealed record StoredRotation(RotationConfiguration Configuration, RotationCheckpoint? Checkpoint);
internal sealed record SaveRequest(RotationConfiguration Configuration, RotationCheckpoint Checkpoint);
internal sealed record LocalRotation(SaveRequest State, MapHistoryEntry[] History, StoredVote[] Votes);
internal sealed record StoredVote(string Map, VoteArchive Result);
internal sealed record CatalogReadDiagnostics(string Source = "Initializing", string ConnectionName = "map_rotation",
    string? Database = null, DateTimeOffset? LastAttemptAtUtc = null,
    DateTimeOffset? LastSuccessAtUtc = null, string? LastErrorType = null);

/// <summary>Один последовательный фоновый worker; игровой поток публикует только immutable-снимки.</summary>
internal sealed class RotationStore(IDbContextFactory<MapRotationDbContext> factory, ILogger<RotationStore> logger,
    string dataDirectory) : IDisposable
{
    private readonly ConcurrentQueue<MapHistoryEntry> _history = new();
    private readonly ConcurrentQueue<(string Map, VoteArchive Result)> _votes = new();
    private readonly CancellationTokenSource _stop = new();
    private Task? _worker;
    private SaveRequest? _pendingSave;
    private StoredRotation? _loaded;
    private RotationConfiguration? _pendingConfig;
    private volatile bool _initialized;
    private int _reload;
    private bool _disposed;
    private bool _databaseReady;
    private CatalogReadDiagnostics _diagnostics = new();
    private string LocalPath => Path.Combine(dataDirectory, "rotation-state.json");
    public bool Initialized => _initialized;
    public StoredRotation? Initial => _loaded;
    public CatalogReadDiagnostics Diagnostics => Volatile.Read(ref _diagnostics);
    public RotationConfiguration? TakeConfiguration() => Interlocked.Exchange(ref _pendingConfig, null);
    public void RequestReload() => Interlocked.Exchange(ref _reload, 1);
    public void Publish(RotationConfiguration config, RotationCheckpoint checkpoint) => Volatile.Write(ref _pendingSave, new(config, checkpoint));
    public void Record(MapHistoryEntry history) => _history.Enqueue(history);
    public void Record(string currentMap, VoteArchive vote) => _votes.Enqueue((currentMap, vote));

    public void Start()
    {
        if (_worker is null) _worker = Task.Run(Run);
    }

    private async Task Run()
    {
        try
        {
            _loaded = ReadLocal();
            Volatile.Write(ref _diagnostics, _diagnostics with { Source = _loaded is null ? "Empty" : "LocalSnapshot" });
            try
            {
                BeginRead();
                await using var db = await factory.CreateDbContextAsync(_stop.Token).ConfigureAwait(false);
                RecordDatabase(db);
                await db.Database.MigrateAsync(_stop.Token).ConfigureAwait(false);
                _databaseReady = true;
                var configuration = await ReadConfiguration(db, _stop.Token).ConfigureAwait(false);
                var persisted = await db.Runtime.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, _stop.Token).ConfigureAwait(false);
                var checkpoint = _loaded?.Checkpoint ?? (persisted is null ? null : JsonSerializer.Deserialize<RotationCheckpoint>(persisted.Checkpoint));
                _loaded = new(configuration, checkpoint);
                ReadSucceeded();
            }
            catch (Exception error) when (!_stop.IsCancellationRequested)
            {
                ReadFailed(error);
                logger.LogError(error, "[MapRotation] БД недоступна; используется последний локальный снимок");
                _loaded ??= new(RotationConfiguration.Empty, null);
            }
            _initialized = true;
            var nextReload = DateTimeOffset.MinValue;
            var nextSave = DateTimeOffset.MinValue;
            SaveRequest? lastSaved = null;
            while (!_stop.IsCancellationRequested)
            {
                var request = Volatile.Read(ref _pendingSave);
                if (request is not null && DateTimeOffset.UtcNow >= nextSave && (!ReferenceEquals(request, lastSaved) || !_history.IsEmpty || !_votes.IsEmpty))
                {
                    WriteLocal(request);
                    try { await Save(request, _stop.Token).ConfigureAwait(false); lastSaved = request; }
                    catch (Exception error) when (!_stop.IsCancellationRequested)
                    { logger.LogWarning(error, "[MapRotation] Снимок сохранён локально; запись в БД будет повторена"); }
                    nextSave = DateTimeOffset.UtcNow.AddSeconds(request.Configuration.Settings.RefreshIntervalSeconds);
                }
                var reloadRequested = Interlocked.Exchange(ref _reload, 0) != 0;
                if (DateTimeOffset.UtcNow >= nextReload || reloadRequested)
                {
                    try
                    {
                        BeginRead();
                        await using var db = await factory.CreateDbContextAsync(_stop.Token).ConfigureAwait(false);
                        RecordDatabase(db);
                        if (!_databaseReady)
                        {
                            await db.Database.MigrateAsync(_stop.Token).ConfigureAwait(false);
                            _databaseReady = true;
                        }
                        var next = await ReadConfiguration(db, _stop.Token).ConfigureAwait(false);
                        // Установленность карты меняется независимо от строк БД.
                        // Каждый успешный refresh повторяет проверку движком на игровом потоке.
                        Volatile.Write(ref _pendingConfig, next);
                        ReadSucceeded();
                    }
                    catch (Exception error) when (!_stop.IsCancellationRequested)
                    { ReadFailed(error); logger.LogWarning(error, "[MapRotation] Ошибка обновления настроек; действующий снимок сохранён"); }
                    nextReload = DateTimeOffset.UtcNow.AddSeconds(request?.Configuration.Settings.RefreshIntervalSeconds ?? 15);
                }
                await Task.Delay(TimeSpan.FromSeconds(1), _stop.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception error) { logger.LogError(error, "[MapRotation] Фоновое сохранение остановлено"); }
    }

    private void BeginRead() => Volatile.Write(ref _diagnostics,
        _diagnostics with { LastAttemptAtUtc = DateTimeOffset.UtcNow });
    private void RecordDatabase(MapRotationDbContext db) => Volatile.Write(ref _diagnostics,
        _diagnostics with { Database = db.Database.GetDbConnection().Database });
    private void ReadSucceeded() => Volatile.Write(ref _diagnostics,
        _diagnostics with { Source = "Database", LastSuccessAtUtc = DateTimeOffset.UtcNow, LastErrorType = null });
    private void ReadFailed(Exception error) => Volatile.Write(ref _diagnostics,
        // Текст исключения и строка подключения не попадают в консольную диагностику.
        _diagnostics with { LastErrorType = error.GetType().Name });

    private static async Task<RotationConfiguration> ReadConfiguration(MapRotationDbContext db, CancellationToken token)
    {
        // RepeatableRead не допускает смешивания settings/maps из разных ревизий.
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, token).ConfigureAwait(false);
        var settings = await db.Settings.AsNoTracking().SingleAsync(x => x.Id == 1, token).ConfigureAwait(false);
        var maps = await db.Maps.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(token).ConfigureAwait(false);
        var snapshot = RotationConfiguration.Create(settings, maps);
        await transaction.CommitAsync(token).ConfigureAwait(false);
        return snapshot;
    }

    private async Task Save(SaveRequest request, CancellationToken token)
    {
        await using var db = await factory.CreateDbContextAsync(token).ConfigureAwait(false);
        var history = _history.ToArray(); var votes = _votes.ToArray();
        await using var transaction = await db.Database.BeginTransactionAsync(token).ConfigureAwait(false);
        var runtime = await db.Runtime.SingleOrDefaultAsync(x => x.Id == 1, token).ConfigureAwait(false);
        if (runtime is null) { runtime = new(); db.Runtime.Add(runtime); }
        runtime.Checkpoint = JsonSerializer.Serialize(request.Checkpoint); runtime.UpdatedAt = DateTimeOffset.UtcNow;
        foreach (var item in history.DistinctBy(item => item.Id))
            if (!await db.History.AnyAsync(x => x.Id == item.Id, token).ConfigureAwait(false)) db.History.Add(new()
            { Id = item.Id, MapId = item.MapId, MapName = item.MapName, WorkshopId = item.WorkshopId, StartedAt = item.StartedAt, EndedAt = item.EndedAt });
        if (request.Checkpoint.Vote is { } active) await SaveVote(db, request.Checkpoint.CurrentMap, active, null, null, token).ConfigureAwait(false);
        foreach (var (map, result) in votes) await SaveVote(db, map, result.Vote, result.FinishedAt, result.WinnerId, token).ConfigureAwait(false);
        await db.SaveChangesAsync(token).ConfigureAwait(false);
        await transaction.CommitAsync(token).ConfigureAwait(false);
        foreach (var unused in history) _history.TryDequeue(out _);
        foreach (var unused in votes) _votes.TryDequeue(out _);
    }

    private static async Task SaveVote(MapRotationDbContext db, string currentMap, VoteState vote, DateTimeOffset? finished,
        long? winner, CancellationToken token)
    {
        var row = db.Votes.Local.FirstOrDefault(x => x.Id == vote.Id)
            ?? await db.Votes.SingleOrDefaultAsync(x => x.Id == vote.Id, token).ConfigureAwait(false);
        if (row is null) { row = new() { Id = vote.Id }; db.Votes.Add(row); }
        row.Source = vote.Source.ToString(); row.CurrentMap = currentMap; row.StartedAt = vote.StartedAt; row.EndsAt = vote.EndsAt;
        row.FinishedAt = finished; row.WinnerMapId = winner;
        foreach (var map in vote.Options)
        {
            var option = await db.Options.FindAsync([vote.Id, map.Id], token).ConfigureAwait(false);
            if (option is null) { option = new() { VoteId = vote.Id, MapId = map.Id }; db.Options.Add(option); }
            option.MapName = map.MapName; option.DisplayName = map.DisplayName;
            option.Votes = vote.Votes.Values.Count(id => id == map.Id);
        }
    }

    private StoredRotation? ReadLocal()
    {
        try
        {
            if (!File.Exists(LocalPath)) return null;
            var local = JsonSerializer.Deserialize<LocalRotation>(File.ReadAllText(LocalPath));
            if (local is null) return null;
            foreach (var entry in local.History) _history.Enqueue(entry);
            foreach (var entry in local.Votes) _votes.Enqueue((entry.Map, entry.Result));
            var saved = local.State;
            return new(RotationConfiguration.Create(saved.Configuration.Settings, saved.Configuration.Maps), saved.Checkpoint);
        }
        catch (Exception error) { logger.LogWarning(error, "[MapRotation] Не удалось прочитать локальный снимок"); return null; }
    }
    private void WriteLocal(SaveRequest request)
    {
        try
        {
            Directory.CreateDirectory(dataDirectory);
            var temporary = LocalPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(new LocalRotation(request, _history.ToArray(),
                _votes.Select(entry => new StoredVote(entry.Map, entry.Result)).ToArray())));
            File.Move(temporary, LocalPath, overwrite: true);
        }
        catch (Exception error) { logger.LogWarning(error, "[MapRotation] Не удалось сохранить локальный снимок"); }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stop.Cancel();
        _worker?.GetAwaiter().GetResult();
        if (Volatile.Read(ref _pendingSave) is { } request)
        {
            WriteLocal(request);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try { Save(request, timeout.Token).GetAwaiter().GetResult(); }
            catch (Exception error) { logger.LogWarning(error, "[MapRotation] Итоговый снимок сохранён локально"); }
        }
        _stop.Dispose();
    }
}
