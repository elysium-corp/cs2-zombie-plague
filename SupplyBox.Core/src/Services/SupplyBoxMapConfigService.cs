using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplyBox.Configuration;
using SupplyBox.Database;
using SupplyBox.Data.Configs;
using SwiftlyS2.Shared;

namespace SupplyBox.Services;

internal sealed class SupplyBoxMapConfigService(ISwiftlyCore core, SupplyBoxRepository repository)
    : IOptions<SupplyBoxConfig>, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly HashSet<Task> _tasks = [];
    private SupplyBoxConfigurationState _state = new(new(0, new()), "loading");
    private int _disposed;
    public SupplyBoxConfig Value => Current.Document.ResolveSettings(MapName);
    public SupplyBoxSnapshot Current => Volatile.Read(ref _state).Snapshot;
    public string MapName { get; private set; } = "";
    public string Source => Volatile.Read(ref _state).Source;

    public SupplyBoxMap? GetMap() => Current.Document.Maps.FirstOrDefault(map =>
        string.Equals(map.Name, MapName, StringComparison.OrdinalIgnoreCase));

    // Выбор текущей карты не запускает скрытый запрос к БД
    public void SetMap(string mapName) => MapName = mapName;

    // Вызывается только при запуске плагина, старте карты и по supply_reload
    public void Refresh()
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        var task = RefreshAsync();
        lock (_tasks) _tasks.Add(task);
        _ = task.ContinueWith(completed => { lock (_tasks) _tasks.Remove(completed); }, TaskScheduler.Default);
    }

    private async Task RefreshAsync()
    {
        var token = _shutdown.Token;
        try
        {
            await _gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                var state = await SupplyBoxConfigurationLoader.LoadAsync(
                    repository.ReadAsync, ReadFallbackAsync, Volatile.Read(ref _state), token).ConfigureAwait(false);
                if (Volatile.Read(ref _disposed) != 0) return;
                Volatile.Write(ref _state, state);
                if (state.DatabaseError is not null)
                {
                    core.Logger.LogWarning(state.DatabaseError,
                        "[SupplyBox] Ошибка загрузки PostgreSQL — источник {Source}, повтор при старте карты или supply_reload", state.Source);
                    if (state.Source != "fallback")
                        core.Logger.LogWarning(state.FallbackError,
                            "[SupplyBox] Fallback отсутствует или некорректен — скачайте supply_box.json в админке и установите на сервер");
                }
                else core.Logger.LogInformation("[SupplyBox] Загружена конфигурация БД версии {Version}", state.Snapshot.Version);
            }
            finally { _gate.Release(); }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (!token.IsCancellationRequested)
                core.Logger.LogError(exception, "[SupplyBox] Загрузка не завершена — предыдущая конфигурация сохранена в памяти");
        }
    }

    private async Task<SupplyBoxDocument?> ReadFallbackAsync(CancellationToken token)
    {
        var path = Path.Combine(core.Configuration.BasePath, "supply_box.json");
        if (!File.Exists(path)) return null;
        if (new FileInfo(path).Length > SupplyBoxDocument.MaximumConfigBytes)
            throw new InvalidDataException("Fallback SupplyBox превышает 8 МБ");
        return SupplyBoxDocument.Parse(await File.ReadAllTextAsync(path, token).ConfigureAwait(false));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _shutdown.Cancel();
        Task[] tasks; lock (_tasks) tasks = _tasks.ToArray();
        try { Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { }
        // Токен и семафор могут ещё использоваться завершающимся запросом PostgreSQL
    }
}
