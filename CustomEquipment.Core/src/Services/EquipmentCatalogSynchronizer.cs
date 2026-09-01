using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Data.Shop;
using CustomEquipment.Database;
using CustomEquipment.Registry;
using Microsoft.Extensions.Logging;

namespace CustomEquipment.Services;

internal sealed class EquipmentCatalogSynchronizer(
    IWeaponCatalogRepository weaponRepository,
    IGameplayItemCatalogRepository gameplayItemRepository,
    IEquipmentShopCatalogRepository shopRepository,
    IItemRegistry itemRegistry,
    GameplayItemCatalog gameplayItemCatalog,
    EquipmentShopRuntimeCatalog shopCatalog,
    ILogger<EquipmentCatalogSynchronizer> logger
) : IDisposable
{
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public bool TryReload(
        out int weaponCount,
        out int gameplayItemCount,
        out int shopListingCount
    )
    {
        weaponCount = 0;
        gameplayItemCount = 0;
        shopListingCount = 0;
        _reloadLock.Wait();

        try
        {
            var weapons = weaponRepository.GetEnabledWeapons();
            var gameplayItems = gameplayItemRepository.GetItems();
            var shopSnapshot = shopRepository.GetSnapshot();

            itemRegistry.ReplaceDatabaseWeapons(weapons);
            gameplayItemCatalog.Replace(gameplayItems);
            shopCatalog.Replace(shopSnapshot);

            weaponCount = weapons.Count;
            gameplayItemCount = gameplayItems.Count(item => item.Enabled);
            shopListingCount = shopSnapshot.Listings.Count(item => item.Enabled);

            logger.LogInformation(
                "Loaded {WeaponCount} database weapons, {GameplayItemCount} enabled gameplay items " +
                "and {ShopListingCount} enabled shop listings from PostgreSQL.",
                weaponCount,
                gameplayItemCount,
                shopListingCount
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
