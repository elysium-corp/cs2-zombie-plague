using CustomKnife.Data.Registrator;
using CustomKnife.Database;
using Microsoft.Extensions.Logging;

namespace CustomKnife.Services;

internal sealed class KnifeCatalogSynchronizer(
    IKnifeCatalogRepository repository,
    IWritableKnivesRegistry registry,
    ILogger<KnifeCatalogSynchronizer> logger
) : IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);
    private readonly Lock _lifecycleLock = new();
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private CancellationTokenSource? _shutdown;
    private Task? _refreshTask;

    public bool TryReload(out int count)
    {
        count = 0;
        _reloadLock.Wait();

        try
        {
            var knives = repository.GetEnabledKnives();
            registry.ReplaceAll(knives);
            count = knives.Count;
            logger.LogInformation("Loaded {KnifeCount} enabled custom knives from PostgreSQL.", count);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to load custom knives. The previous in-memory snapshot is still active."
            );
            return false;
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public void Start()
    {
        lock (_lifecycleLock)
        {
            if (_refreshTask is not null)
            {
                return;
            }

            _shutdown = new CancellationTokenSource();
            _refreshTask = Task.Run(() => RefreshLoopAsync(_shutdown.Token));
        }
    }

    public void Stop()
    {
        Task? refreshTask;
        CancellationTokenSource? shutdown;

        lock (_lifecycleLock)
        {
            refreshTask = _refreshTask;
            shutdown = _shutdown;
            _refreshTask = null;
            _shutdown = null;
        }

        if (refreshTask is null || shutdown is null)
        {
            return;
        }

        shutdown.Cancel();

        try
        {
            refreshTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
        }
        finally
        {
            shutdown.Dispose();
        }
    }

    public void Dispose()
    {
        Stop();
        _reloadLock.Dispose();
    }

    private async Task RefreshLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(RefreshInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                TryReload(out _);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
