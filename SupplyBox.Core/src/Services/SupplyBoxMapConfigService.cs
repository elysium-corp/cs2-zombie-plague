using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplyBox.Configuration;
using SupplyBox.Database;
using SupplyBox.Data.Configs;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;

namespace SupplyBox.Services;

internal sealed class SupplyBoxMapConfigService(ISwiftlyCore core, SupplyBoxRepository repository)
    : IOptions<SupplyBoxConfig>, IDisposable
{
    internal const int MaximumConfigBytes = SupplyBoxDocument.MaximumConfigBytes;
    internal const int MaximumPoints = 512;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly HashSet<Task> _tasks = [];
    private SupplyBoxSnapshot _snapshot = new(0, new());
    private int _disposed;
    public SupplyBoxConfig Value => Current.Document.Settings;
    public SupplyBoxSnapshot Current => Volatile.Read(ref _snapshot);
    public string MapName { get; private set; } = "";
    public string Source { get; private set; } = "loading";
    public bool DatabaseAvailable { get; private set; }

    public SupplyBoxMap? GetMap() => Current.Document.Maps.FirstOrDefault(map =>
        string.Equals(map.Name, MapName, StringComparison.OrdinalIgnoreCase));

    public void LoadConfig(string mapName)
    {
        MapName = mapName;
        Track(RefreshAsync());
    }

    public void Refresh() => Track(RefreshAsync());

    public IReadOnlyList<SupplyBoxEntityConfig> GetSnapshot() => GetMap()?.Points
        .Select(point => new SupplyBoxEntityConfig
        {
            Index = point.Id, Position = new((float)point.X, (float)point.Y, (float)point.Z),
            Rotation = new((float)point.Pitch, (float)point.Yaw, (float)point.Roll)
        }).ToArray() ?? [];

    private async Task RefreshAsync()
    {
        var token = _shutdown.Token;
        try
        {
            await _gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                var fallback = await ReadFallbackAsync(token).ConfigureAwait(false);
                try
                {
                    await repository.InitializeAsync(fallback ?? new(), token).ConfigureAwait(false);
                    var loaded = await repository.ReadAsync(token).ConfigureAwait(false);
                    if (!loaded.LegacyImported)
                    {
                        ImportLegacyMaps(loaded.Document);
                        if (!await repository.SaveAsync(loaded with { LegacyImported = true }, token).ConfigureAwait(false))
                            throw new InvalidOperationException("SupplyBox changed during legacy import; reload will retry.");
                        loaded = await repository.ReadAsync(token).ConfigureAwait(false);
                    }
                    Publish(loaded, "database");
                    DatabaseAvailable = true;
                }
                catch (Exception exception) when (!token.IsCancellationRequested)
                {
                    DatabaseAvailable = false;
                    if (fallback is not null) Publish(new(0, fallback), "fallback");
                    else if (Source == "loading") Publish(new(0, new()), "defaults");
                    else Publish(Current, "memory");
                    core.Logger.LogWarning(exception, "[SupplyBox] PostgreSQL unavailable; using {Source}. No files will be written.", Source);
                }
            }
            finally { _gate.Release(); }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { if (!token.IsCancellationRequested) core.Logger.LogError(exception, "[SupplyBox] Reload failed; previous snapshot retained."); }
    }

    public Task<bool> AddAsync(Vector position, Vector rotation)
    {
        var mapName = MapName;
        return ChangeAsync(document =>
        {
            var map = document.Maps.FirstOrDefault(item => string.Equals(item.Name, mapName, StringComparison.OrdinalIgnoreCase));
            if (map is null) { map = new() { Name = mapName }; document.Maps.Add(map); }
            if (map.Points.Count >= MaximumPoints) return false;
            var id = Enumerable.Range(1, int.MaxValue).First(id => map.Points.All(point => point.Id != id));
            map.Points.Add(new() { Id = id, Name = $"Точка {id}", X = position.X, Y = position.Y,
                Z = position.Z, Pitch = rotation.X, Yaw = rotation.Y, Roll = rotation.Z });
            return true;
        });
    }

    public Task<bool> RemoveAsync(int index)
    {
        var mapName = MapName;
        return ChangeAsync(document => document.Maps.FirstOrDefault(map => string.Equals(map.Name, mapName, StringComparison.OrdinalIgnoreCase))?.Points.RemoveAll(point => point.Id == index) > 0);
    }

    public void DiscoverPoints(string mapName, IReadOnlyList<SupplyBoxPoint> points)
    {
        if (points.Count == 0 || !Value.AutoDiscoverSpawnPoints || Current.Document.Maps.Any(map => string.Equals(map.Name, mapName, StringComparison.OrdinalIgnoreCase))) return;
        if (DatabaseAvailable)
        {
            Track(ChangeAsync(document =>
            {
                if (document.Maps.Any(map => string.Equals(map.Name, mapName, StringComparison.OrdinalIgnoreCase))) return false;
                document.Maps.Add(new() { Name = mapName, Points = points.ToList() });
                return true;
            }));
        }
        else
        {
            var snapshot = Current;
            var document = snapshot.Document.Clone();
            document.Maps.Add(new() { Name = mapName, Points = points.ToList() });
            Publish(snapshot with { Document = document }, Source);
        }
    }

    private Task<bool> ChangeAsync(Func<SupplyBoxDocument, bool> change)
    {
        var task = ChangeCoreAsync(change);
        Track(task);
        return task;
    }

    private async Task<bool> ChangeCoreAsync(Func<SupplyBoxDocument, bool> change)
    {
        if (Volatile.Read(ref _disposed) != 0) return false;
        var token = _shutdown.Token;
        try
        {
            await _gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    var snapshot = await repository.ReadAsync(token).ConfigureAwait(false);
                    if (!change(snapshot.Document)) return false;
                    if (!await repository.SaveAsync(snapshot, token).ConfigureAwait(false)) continue;
                    Publish(snapshot with { Version = snapshot.Version + 1 }, "database");
                    DatabaseAvailable = true;
                    return true;
                }
            }
            finally { _gate.Release(); }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (!token.IsCancellationRequested)
                core.Logger.LogError(exception, "[SupplyBox] Point was NOT saved. Database writes are required for editing.");
        }
        return false;
    }

    private async Task<SupplyBoxDocument?> ReadFallbackAsync(CancellationToken token)
    {
        var path = Path.Combine(core.Configuration.BasePath, "supply_box.json");
        if (!File.Exists(path)) return null;
        try
        {
            if (new FileInfo(path).Length > MaximumConfigBytes) throw new InvalidDataException("SupplyBox fallback exceeds 8 MiB.");
            var json = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
            using var parsed = JsonDocument.Parse(json, new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            if (parsed.RootElement.TryGetProperty("SupplyBox", out var legacy))
            {
                var document = new SupplyBoxDocument
                {
                    Settings = legacy.Deserialize<SupplyBoxConfig>(SupplyBoxDocument.Json) ?? new()
                };
                ImportLegacyMaps(document);
                document.Validate();
                return document;
            }
            return SupplyBoxDocument.Parse(json);
        }
        catch (Exception exception) when (!token.IsCancellationRequested)
        {
            core.Logger.LogWarning(exception, "[SupplyBox] Invalid fallback ignored; database loading continues.");
            return null;
        }
    }

    private void ImportLegacyMaps(SupplyBoxDocument document)
    {
        var directory = Path.Combine(core.PluginPath, "SupplyBox");
        if (!Directory.Exists(directory)) return;
        foreach (var path in Directory.EnumerateFiles(directory, "*.json").Take(512))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (document.Maps.Any(map => string.Equals(map.Name, name, StringComparison.OrdinalIgnoreCase))) continue;
            try
            {
                if (new FileInfo(path).Length > 1_048_576) throw new InvalidDataException("Legacy map exceeds 1 MiB.");
                using var json = JsonDocument.Parse(File.ReadAllText(path));
                var map = new SupplyBoxMap { Name = name };
                foreach (var point in json.RootElement.GetProperty("SupplyBoxes").EnumerateArray())
                {
                    var position = point.GetProperty("Position"); var rotation = point.GetProperty("Rotation");
                    var id = point.GetProperty("Index").GetInt32();
                    map.Points.Add(new() { Id = id, Name = $"Точка {id}", X = position.GetProperty("X").GetDouble(),
                        Y = position.GetProperty("Y").GetDouble(), Z = position.GetProperty("Z").GetDouble(),
                        Pitch = rotation.GetProperty("X").GetDouble(), Yaw = rotation.GetProperty("Y").GetDouble(), Roll = rotation.GetProperty("Z").GetDouble() });
                }
                // Старый плагин автоматически создавал пустые файлы для каждой карты.
                // Они не обозначают намеренное отключение карты в новой конфигурации.
                if (map.Points.Count == 0) continue;
                var candidate = document.Clone(); candidate.Maps.Add(map); candidate.Validate();
                document.Maps.Add(map);
            }
            catch (Exception exception) { core.Logger.LogWarning(exception, "[SupplyBox] Could not import legacy map {Map}; original file retained.", name); }
        }
    }

    private void Publish(SupplyBoxSnapshot snapshot, string source)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        Volatile.Write(ref _snapshot, snapshot);
        Source = source;
    }

    private void Track(Task task)
    {
        lock (_tasks) _tasks.Add(task);
        _ = task.ContinueWith(completed => { lock (_tasks) _tasks.Remove(completed); }, TaskScheduler.Default);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _shutdown.Cancel();
        Task[] tasks; lock (_tasks) tasks = _tasks.ToArray();
        try { Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { }
        // Токен и семафор могут ещё использоваться завершающимся запросом PostgreSQL.
    }
}
