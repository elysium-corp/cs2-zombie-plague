using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Shop;

internal interface IInstantEquipmentShopItem
{
    bool TryGrant(IPlayer player);
}
