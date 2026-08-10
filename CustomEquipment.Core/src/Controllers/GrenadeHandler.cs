using CustomEquipment.Api;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Events;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Controllers;

internal class GrenadeHandler(IEventPublisher eventPublisher)
{
    private readonly Dictionary<IPlayer, List<GrenadeEntry>> _grenades = [];

    private sealed record GrenadeEntry(CBaseCSGrenadeProjectile Projectile, IGrenade Grenade);
    
    internal void OnGrenadeThrown(IGrenade grenade, CBaseCSGrenadeProjectile projectile)
    {
        var thrower = projectile.OriginalThrower.Value?.ToPlayer();
        
        if (thrower == null) return;

        AddThrownGrenade(thrower, projectile, grenade);
    }

    internal void OnGrenadeDetonated(IGrenade grenade, CBaseCSGrenadeProjectile projectile, Vector position)
    {
        var thrower = projectile.OriginalThrower.Value?.ToPlayer();

        if (thrower == null) return;
        
        RemoveThrownGrenade(thrower, projectile);
    }

    internal void OnTick()
    {
        foreach (var (player, grenadeEntries) in _grenades.ToArray())
        {
            foreach (var grenadeEntry in grenadeEntries.ToArray())
            {
                var projectile = grenadeEntry.Projectile;
                var grenade = grenadeEntry.Grenade;
                
                if (!projectile.IsValidEntity)
                {
                    continue;
                }
                var position = projectile.AbsOrigin;
                
                if (projectile.DetonationRecorded && position != null)
                {
                    eventPublisher.OnGrenadeDetonated(grenade, projectile, position.Value);
                }
            }
        }
    }
    
    private void AddThrownGrenade(IPlayer thrower, CBaseCSGrenadeProjectile projectile, IGrenade grenade)
    {
        if (!_grenades.TryGetValue(thrower, out var thrownGrenades))
        {
            thrownGrenades = [];
            _grenades[thrower] = thrownGrenades;
        }
        
        thrownGrenades.Add(new GrenadeEntry(projectile, grenade));
    }

    private void RemoveThrownGrenade(IPlayer thrower, CBaseCSGrenadeProjectile projectile)
    {
        if (!_grenades.TryGetValue(thrower, out var thrownGrenades)) return;
        
        thrownGrenades.RemoveAll(entry => entry.Projectile == projectile);

        if (thrownGrenades.Count == 0) _grenades.Remove(thrower);
    }
}