using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace ZombiePlague.Core.Catalog;

internal sealed class ZombieCatalogService(ISwiftlyCore core, ZombieCatalogRepository repository) : IDisposable
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _shutdown = new();
    private Task _refresh = Task.CompletedTask;
    private ZombieCatalogState? _state;
    private ZombieCatalogState? _pendingResources;
    private HashSet<string>? _mapResources;
    private bool _disposed;

    public ZombieCatalogState Current => Volatile.Read(ref _state)
        ?? throw new InvalidOperationException("Каталог классов ещё не загружен");

    public long? PendingResourceVersion { get { lock (_gate) return _pendingResources?.Version; } }

    public string[] PrepareResourcesForMap()
    {
        lock (_gate)
        {
            if (_pendingResources is not null)
            {
                Volatile.Write(ref _state, _pendingResources);
                _pendingResources = null;
            }
            var resources = Current.Document.Resources().ToArray();
            _mapResources = resources.ToHashSet(StringComparer.Ordinal);
            return resources;
        }
    }

    public void Initialize()
    {
        RefreshAsync().GetAwaiter().GetResult();
        _ = Current;
    }

    // Одновременно выполняется не более одного запроса, частые команды не создают очередь задач
    public Task RefreshAsync()
    {
        lock (_gate)
        {
            if (_disposed) return Task.CompletedTask;
            if (!_refresh.IsCompleted) return _refresh;
            return _refresh = Task.Run(LoadAsync);
        }
    }

    private async Task LoadAsync()
    {
        var token = _shutdown.Token;
        try
        {
            var state = await ZombieCatalogLoader.LoadAsync(repository.ReadAsync, ReadFallbackAsync,
                Volatile.Read(ref _state), token).ConfigureAwait(false);
            lock (_gate)
            {
                if (_disposed) return;
                // Новые модели и частицы применяются после регистрации в manifest следующей карты
                if (_mapResources is not null && state.Document.Resources().Any(resource => !_mapResources.Contains(resource)))
                {
                    _pendingResources = state;
                    core.Logger.LogInformation("[ZombieCatalog] Версия {Version} загружена и ожидает начала карты для регистрации новых ресурсов", state.Version);
                }
                else
                {
                    _pendingResources = null;
                    Volatile.Write(ref _state, state);
                }
                if (state.DatabaseError is not null)
                    ZombieCatalogDiagnostics.LogDatabaseFailure(core.Logger, state.DatabaseError, state.Source);
                else
                    core.Logger.LogInformation("[ZombieCatalog] Загружена версия {Version}: классов {Classes}, способностей {Abilities}",
                        state.Version, state.Document.Classes.Count, state.Document.Abilities.Count);
                if (state.FallbackError is not null)
                    core.Logger.LogError(state.FallbackError, "[ZombieCatalog] Некорректный fallback — сохранён предыдущий каталог");
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception)
        {
            lock (_gate)
                if (!_disposed) core.Logger.LogError(exception, "[ZombieCatalog] Не удалось загрузить каталог БД и fallback");
        }
    }

    private Task<ZombieCatalogDocument> ReadFallbackAsync(CancellationToken token) => CatalogFallback.ReadAsync(core, token);

    public void Dispose()
    {
        Task task;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            task = _refresh;
        }
        _shutdown.Cancel();
        // Запрос ограничен тайм-аутом и отменой, после завершения не остаётся обращений к Core
        task.GetAwaiter().GetResult();
        _shutdown.Dispose();
    }
}
