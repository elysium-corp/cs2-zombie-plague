using CustomEquipment.Api.Data;
using Shop.Api.Data;

namespace Shop.Core.Services;

internal interface IShopCatalog
{
    IReadOnlyCollection<EquipmentCategory> GetCategories();

    IReadOnlyCollection<ShopItem> GetItems();

    IReadOnlyCollection<ShopItem> GetItems(EquipmentCategory category);

    bool TryGetItem(string itemId, out ShopItem? item);
}
