using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api.Events;

public interface IEventPublisher
{
    void OnItemBought(IPlayer player, IShopItem item);
    
    void OnItemGiven(IPlayer player, IItem item);
    
    void OnWeaponGiven(IPlayer player, IWeapon weapon);
    
    void OnGrenadeGiven(IPlayer player, IGrenade grenade);
    
    void OnGrenadeThrown(IGrenade grenade, CBaseCSGrenadeProjectile projectile);

    void OnGrenadeDetonated(IGrenade grenade, CBaseCSGrenadeProjectile projectile, Vector position);
    void OnMinePlaced(IPlayer player, LaserMineEntityBase mine);
}