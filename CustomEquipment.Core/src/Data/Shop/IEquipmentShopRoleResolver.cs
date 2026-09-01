using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Shop;

internal interface IEquipmentShopRoleResolver
{
    EquipmentShopType GetShopType(IPlayer player);
}
