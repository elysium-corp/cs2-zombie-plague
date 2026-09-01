using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;

namespace CustomEquipment.Data.Shop;

internal sealed class EquipmentShopRoleResolver(
    Func<IZombiePlagueApi> zombiePlagueApi
) : IEquipmentShopRoleResolver
{
    public EquipmentShopType GetShopType(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return zombiePlagueApi().IsInfected(player)
            ? EquipmentShopType.Zombie
            : EquipmentShopType.Human;
    }
}
