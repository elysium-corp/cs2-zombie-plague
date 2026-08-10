using CustomEquipment.Api.Data;
using Shop.Api;
using Shop.Api.Data;
using Shop.Core.Menus;
using Shop.Core.Services;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.SharedApi;

internal sealed class ShopApi(
    Lazy<IShopCatalog> catalog,
    Lazy<IShopPurchaseService> purchaseService,
    Lazy<ShopMenu> menu
) : IShopApi
{
    public IReadOnlyCollection<EquipmentCategory> GetCategories() => catalog.Value.GetCategories();

    public IReadOnlyCollection<ShopItem> GetItems() => catalog.Value.GetItems();

    public IReadOnlyCollection<ShopItem> GetItems(EquipmentCategory category) =>
        catalog.Value.GetItems(category);

    public ShopPurchaseResult TryPurchase(IPlayer player, string itemId) =>
        purchaseService.Value.TryPurchase(player, itemId);

    public void Open(IPlayer player) => menu.Value.Open(player);
}
