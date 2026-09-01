using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Database;
using CustomEquipment.Registry;
using Microsoft.Extensions.Logging;

namespace CustomEquipment.Services;

internal sealed class EquipmentCatalogSynchronizer(
    IWeaponCatalogRepository weaponRepository,
    IGameplayItemCatalogRepository gameplayItemRepository,
    IItemRegistry itemRegistry,
    GameplayItemCatalog gameplayItemCatalog,
    ILogger<EquipmentCatalogSynchronizer> logger
) : IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);
    private readonly Lock _lifecycleLock = new();
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private CancellationTokenSource? _shutdown;
    private Task? _refreshTask;

    public bool TryReload(out int weaponCount, out int gameplayItemCount)
    {
        weaponCount = 0;
        gameplayItemCount = 0;
        _reloadLock.Wait();

        try
        {
            var weapons = weaponRepository.GetEnabledWeapons();
            var gameplayItems = gameplayItemRepository.GetItems();

            itemRegistry.ReplaceDatabaseWeapons(weapons);
            gameplayItemCatalog.Replace(gameplayItems);

            weaponCount = weapons.Count;
            gameplayItemCount = gameplayItems.Count(item => item.Enabled);

            logger.LogInformation(
                "Loaded {WeaponCount} database weapons and {GameplayItemCount} enabled gameplay items from PostgreSQL.",
                weaponCount,
                gameplayItemCount
            );
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to refresh custom equipment. Previous in-memory snapshots remain active."
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
                TryReload(out _, out _);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
