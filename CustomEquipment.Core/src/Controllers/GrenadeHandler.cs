using CustomEquipment.Api.Data.Contracts;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Controllers;

internal sealed class GrenadeHandler
{
    private readonly Dictionary<IPlayer, List<GrenadeEntry>> _grenades = [];

    private sealed record GrenadeEntry(CBaseCSGrenadeProjectile Projectile, IGrenade Grenade);

    internal void OnGrenadeThrown(IGrenade grenade, CBaseCSGrenadeProjectile projectile)
    {
        var thrower = projectile.OriginalThrower.Value?.ToPlayer();

        if (thrower == null) return;

        AddThrownGrenade(thrower, projectile, grenade);
    }

    internal void OnTick(Action<IGrenade, CBaseCSGrenadeProjectile, Vector> onDetonated)
    {
        foreach (var (player, grenadeEntries) in _grenades.ToArray())
        {
            foreach (var grenadeEntry in grenadeEntries.ToArray())
            {
                var projectile = grenadeEntry.Projectile;

                if (!projectile.IsValidEntity)
                {
                    RemoveThrownGrenade(player, projectile);
                    continue;
                }

                var position = projectile.AbsOrigin;

                if (!projectile.DetonationRecorded || position == null)
                {
                    continue;
                }

                RemoveThrownGrenade(player, projectile);
                onDetonated(grenadeEntry.Grenade, projectile, position.Value);
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

        if (thrownGrenades.Count == 0)
        {
            _grenades.Remove(thrower);
        }
    }
}
