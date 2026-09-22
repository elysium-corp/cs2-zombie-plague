using CustomKnife.Data.Models;
using CustomKnife.Data.Registrator;
using CustomKnife.Database;
using CustomKnife.Hud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomKnife.Services;

internal sealed class KnifeCatalogSynchronizer(
    IKnifeCatalogRepository repository,
    IWritableKnivesRegistry registry,
    IOptions<KnifeHudOptions> options,
    ILogger<KnifeCatalogSynchronizer> logger
) : IDisposable
{
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private IReadOnlyCollection<IKnife>? _snapshot;
    private CancellationTokenSource? _stop;
    private Task? _polling;
    private long _revision;

    public long Revision => Interlocked.Read(ref _revision);

    public void Start()
    {
        if (_polling is not null || options.Value.CatalogRefreshSeconds <= 0) return;
        _stop = new CancellationTokenSource();
        var token = _stop.Token;
        var interval = TimeSpan.FromSeconds(Math.Clamp(options.Value.CatalogRefreshSeconds, 1, 300));
        _polling = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(interval);
            try
            {
                while (await timer.WaitForNextTickAsync(token)) await ReloadAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }, token);
    }

    public void Stop()
    {
        _stop?.Cancel();
        try { _polling?.GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { }
        _polling = null;
        _stop?.Dispose();
        _stop = null;
    }

    public bool TryReload(out int count)
    {
        count = 0;
        _reloadLock.Wait();

        try
        {
            var knives = repository.GetEnabledKnives();
            Publish(knives);
            count = knives.Count;
            return true;
        }
        catch (Exception exception)
        {
            Failed(exception);
            return false;
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    internal async Task<bool> ReloadAsync(CancellationToken token)
    {
        await _reloadLock.WaitAsync(token);
        try
        {
            var knives = await repository.GetEnabledKnivesAsync(token);
            token.ThrowIfCancellationRequested();
            Publish(knives);
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception exception) { Failed(exception); return false; }
        finally { _reloadLock.Release(); }
    }

    private void Publish(IReadOnlyCollection<IKnife> knives)
    {
        // Неизменившийся каталог сохраняет объекты, сущность HUD и захват мыши.
        if (_snapshot is not null && _snapshot.SequenceEqual(knives)) return;
        registry.ReplaceAll(knives);
        _snapshot = knives;
        Interlocked.Increment(ref _revision);
        logger.LogInformation("Loaded {KnifeCount} enabled custom knives from PostgreSQL.", knives.Count);
    }

    private void Failed(Exception exception) => logger.LogError(exception,
        "Failed to load custom knives. The previous in-memory snapshot is still active.");

    public void Dispose()
    {
        Stop();
        _reloadLock.Dispose();
    }
}
