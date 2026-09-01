using CustomEquipment.Data.Shop;

namespace CustomEquipment.Database;

internal interface IEquipmentShopCatalogRepository
{
    EquipmentShopSnapshot GetSnapshot();
}
