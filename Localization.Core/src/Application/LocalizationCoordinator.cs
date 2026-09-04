using Localization.Core.Data;

namespace Localization.Core.Application;

internal sealed class LocalizationCoordinator(
    LocalizationCache cache,
    DatabaseLocalizationProvider databaseProvider,
    FallbackLocalizationProvider fallbackProvider,
    RateLimitedLocalizationLogger rateLimitedLogger) : IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private readonly object _taskSync = new();
    private readonly HashSet<Task> _tasks = [];
    private int _stopped;

    public void Start()
    {
        Track(ReloadFromSourcesAsync(_lifetime.Token));
    }

    public void OnMapEnded()
    {
        if (Volatile.Read(ref _stopped) != 0)
        {
            return;
        }

        Track(ReloadFromSourcesAsync(_lifetime.Token));
    }

    public Task<(bool Success, string Message)> ReloadNowAsync()
    {
        var task = ReloadFromSourcesAsync(_lifetime.Token);
        Track(task);
        return task;
    }

    private async Task<(bool Success, string Message)> ReloadFromSourcesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _reloadLock.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return (false, "Localization reload cancelled.");
        }

        try
        {
            try
            {
                var databaseSnapshot = await databaseProvider.LoadAsync(cancellationToken);
                cache.Replace(databaseSnapshot);
                return (
                    true,
                    $"Snapshot из PostgreSQL обновлён: {databaseSnapshot.Entries.Count} ключей.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return (false, "Localization reload cancelled.");
            }
            catch (Exception databaseException)
            {
                try
                {
                    var configSnapshot = fallbackProvider.Load();
                    cache.Replace(configSnapshot);
                    rateLimitedLogger.Warning(
                        "database:fallback-config",
                        TimeSpan.FromMinutes(2),
                        "[Localization] PostgreSQL недоступен или snapshot невалиден: {DatabaseError}. " +
                        "В memory cache загружен localization.json.",
                        databaseException.Message);
                    return (
                        true,
                        $"PostgreSQL недоступен; загружен localization.json: {configSnapshot.Entries.Count} ключей.");
                }
                catch (Exception configException)
                {
                    rateLimitedLogger.Warning(
                        "sources:unavailable",
                        TimeSpan.FromMinutes(2),
                        "[Localization] Не удалось обновить memory cache. PostgreSQL: {DatabaseError}. " +
                        "localization.json: {ConfigError}. Текущий snapshot сохранён без изменений.",
                        databaseException.Message,
                        configException.Message);
                    return (
                        false,
                        "PostgreSQL и localization.json недоступны; текущий snapshot сохранён.");
                }
            }
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private void Track(Task task)
    {
        lock (_taskSync)
        {
            _tasks.Add(task);
        }

        _ = task.ContinueWith(
            completed =>
            {
                lock (_taskSync)
                {
                    _tasks.Remove(completed);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        _lifetime.Cancel();
        Task[] tasks;
        lock (_taskSync)
        {
            tasks = _tasks.ToArray();
        }

        try
        {
            Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(10));
        }
        catch (AggregateException exception) when (
            exception.InnerExceptions.All(inner => inner is OperationCanceledException))
        {
        }

        // Не освобождаем примитивы синхронизации: провайдер БД может завершить
        // отменённую операцию уже после таймаута горячей выгрузки плагина.
    }
}
