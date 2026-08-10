using CustomEquipment.Api.Data;
using Shop.Api.Data;
using SwiftlyS2.Shared.Players;

namespace Shop.Api;

public interface IShopApi
{
    IReadOnlyCollection<EquipmentCategory> GetCategories();

    IReadOnlyCollection<ShopItem> GetItems();

    IReadOnlyCollection<ShopItem> GetItems(EquipmentCategory category);

    ShopPurchaseResult TryPurchase(IPlayer player, string itemId);

    void Open(IPlayer player);

    static readonly string SharedApiKey = "Shop.Api.IShopApi";
}
