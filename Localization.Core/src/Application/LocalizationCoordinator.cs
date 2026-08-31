using Localization.Core.Data;
using Microsoft.Extensions.Logging;

namespace Localization.Core.Application;

internal sealed class LocalizationCoordinator(
    LocalizationCache cache,
    DatabaseLocalizationProvider databaseProvider,
    FallbackLocalizationProvider fallbackProvider,
    RateLimitedLocalizationLogger rateLimitedLogger,
    ILogger logger) : IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private readonly object _taskSync = new();
    private readonly HashSet<Task> _tasks = [];
    private LocalizationSnapshot _fallback = EmergencyLocalizationSnapshot.Create();
    private long _nextReloadUnixMilliseconds;
    private int _stopped;

    public void Start()
    {
        try
        {
            _fallback = fallbackProvider.Load();
            if (_fallback.Source == LocalizationSource.Emergency)
            {
                logger.LogWarning(
                    "[Localization] localization.json отсутствует или содержит пустой шаблон. " +
                    "До загрузки PostgreSQL используется встроенный snapshot.");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "[Localization] Fallback-конфигурация не прошла валидацию. Используется аварийный snapshot.");
        }

        cache.Replace(_fallback);
        ScheduleNext(_fallback.Settings.RefreshIntervalSeconds);
        Track(ReloadDatabaseAsync("запуск плагина", _lifetime.Token));
    }

    public void Tick()
    {
        if (Volatile.Read(ref _stopped) != 0
            || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < Interlocked.Read(ref _nextReloadUnixMilliseconds)
            || _reloadLock.CurrentCount == 0)
        {
            return;
        }

        ScheduleNext(cache.Current?.Settings.RefreshIntervalSeconds ?? 30);
        Track(ReloadDatabaseAsync("периодическая проверка", _lifetime.Token));
    }

    public Task<(bool Success, string Message)> ReloadNowAsync()
    {
        var task = ReloadDatabaseAsync("команда localization_reload", _lifetime.Token);
        Track(task);
        return task;
    }

    private async Task<(bool Success, string Message)> ReloadDatabaseAsync(
        string reason,
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
            var snapshot = await databaseProvider.LoadAsync(cancellationToken);
            cache.Replace(snapshot);
            ScheduleNext(snapshot.Settings.RefreshIntervalSeconds);
            logger.LogInformation(
                "[Localization] Загружено {Entries} ключей и {Languages} языков из PostgreSQL. Причина: {Reason}.",
                snapshot.Entries.Count,
                snapshot.Languages.Count,
                reason);
            return (true, $"Snapshot обновлён: {snapshot.Entries.Count} ключей.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return (false, "Localization reload cancelled.");
        }
        catch (Exception exception)
        {
            var current = cache.Current;
            if (current?.Settings.LocalCacheEnabled == true)
            {
                if (current.Source == LocalizationSource.Database)
                {
                    cache.Replace(current.AsCache());
                }
            }
            else
            {
                cache.Replace(_fallback);
            }

            ScheduleNext(cache.Current?.Settings.RefreshIntervalSeconds ?? 30);
            rateLimitedLogger.Warning(
                "database:unavailable",
                TimeSpan.FromMinutes(2),
                "[Localization] PostgreSQL недоступен или snapshot невалиден: {Error}. Сохранён LKG/fallback.",
                exception.Message);
            return (false, "Reload failed. LKG/fallback preserved.");
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

    private void ScheduleNext(int seconds)
    {
        Interlocked.Exchange(
            ref _nextReloadUnixMilliseconds,
            DateTimeOffset.UtcNow.AddSeconds(Math.Max(5, seconds)).ToUnixTimeMilliseconds());
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
