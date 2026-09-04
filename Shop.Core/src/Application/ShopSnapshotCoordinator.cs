using Microsoft.Extensions.Logging;
using Shop.Core.Data;
using Shop.Core.Database;

namespace Shop.Core.Application;

internal sealed class ShopSnapshotCoordinator(
    ShopSnapshotCache cache,
    ShopSnapshotRepository database,
    FallbackShopSnapshotProvider fallback,
    ILogger<ShopSnapshotCoordinator> logger) : IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private readonly object _tasksSync = new();
    private readonly HashSet<Task> _tasks = [];
    private int _stopped;

    public void Start() => Track(ReloadAsync(_lifetime.Token));

    public void ReloadAtMapEnd()
    {
        if (Volatile.Read(ref _stopped) == 0)
        {
            Track(ReloadAsync(_lifetime.Token));
        }
    }

    public Task<bool> ReloadNowAsync()
    {
        var task = ReloadAsync(_lifetime.Token);
        Track(task);
        return task;
    }

    private async Task<bool> ReloadAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _reloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        try
        {
            try
            {
                var snapshot = await database.LoadAsync(cancellationToken).ConfigureAwait(false);
                cache.Replace(snapshot);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception databaseException)
            {
                try
                {
                    var snapshot = fallback.Load();
                    cache.Replace(snapshot);
                    logger.LogWarning(
                        databaseException,
                        "[Shop] PostgreSQL недоступен; в memory snapshot загружен shop.json.");
                    return true;
                }
                catch (Exception fallbackException)
                {
                    logger.LogError(
                        fallbackException,
                        "[Shop] PostgreSQL и shop.json недоступны. Текущий memory snapshot сохранён. " +
                        "Ошибка PostgreSQL: {DatabaseError}",
                        databaseException.Message);
                    return false;
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
        lock (_tasksSync)
        {
            _tasks.Add(task);
        }

        _ = task.ContinueWith(
            completed =>
            {
                lock (_tasksSync)
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
        lock (_tasksSync)
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
    }
}
