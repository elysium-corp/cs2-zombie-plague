using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api.Events;

public class EventDelegates
{
    public delegate void OnItemGiven(IPlayer player, IItem item);
    
    public delegate void OnWeaponGiven(IPlayer player, IWeapon weapon);
    
    public delegate void OnGrenadeGiven(IPlayer player, IGrenade grenade);
    
    public delegate void OnGrenadeThrown(IGrenade grenade, CBaseCSGrenadeProjectile projectile);

    public delegate void OnGrenadeDetonated(IGrenade grenade, CBaseCSGrenadeProjectile projectile, Vector position);
}