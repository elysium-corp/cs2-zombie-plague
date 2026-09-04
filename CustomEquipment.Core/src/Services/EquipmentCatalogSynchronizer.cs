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
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public bool TryReload(
        out int weaponCount,
        out int gameplayItemCount
    )
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
                "Loaded {WeaponCount} database weapons and {GameplayItemCount} enabled gameplay items " +
                "from PostgreSQL.",
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

    public void Dispose()
    {
        _reloadLock.Dispose();
    }
}
