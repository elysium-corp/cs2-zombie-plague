using CustomEquipment.Api.Data.Contracts;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Controllers;

internal sealed class GrenadeHandler
{
    private const int MaximumGrenadesPerPlayer = 64;
    private const int MaximumTrackedGrenades = 512;
    private readonly Dictionary<IPlayer, List<GrenadeEntry>> _grenades = [];
    private readonly List<IPlayer> _players = [];
    private int _trackedCount;

    private sealed record GrenadeEntry(CBaseCSGrenadeProjectile Projectile, IGrenade Grenade);

    internal void OnGrenadeThrown(IGrenade grenade, CBaseCSGrenadeProjectile projectile)
    {
        var thrower = projectile.OriginalThrower.Value?.ToPlayer();

        if (thrower == null) return;

        AddThrownGrenade(thrower, projectile, grenade);
    }

    internal void OnTick(Action<IGrenade, CBaseCSGrenadeProjectile, Vector> onDetonated)
    {
        _players.Clear();
        _players.AddRange(_grenades.Keys);

        foreach (var player in _players)
        {
            if (!_grenades.TryGetValue(player, out var grenadeEntries)) continue;

            for (var index = grenadeEntries.Count - 1; index >= 0; index--)
            {
                var grenadeEntry = grenadeEntries[index];
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
        if (_trackedCount >= MaximumTrackedGrenades) return;

        if (!_grenades.TryGetValue(thrower, out var thrownGrenades))
        {
            thrownGrenades = [];
            _grenades[thrower] = thrownGrenades;
        }

        if (thrownGrenades.Count >= MaximumGrenadesPerPlayer) return;

        thrownGrenades.Add(new GrenadeEntry(projectile, grenade));
        _trackedCount++;
    }

    private void RemoveThrownGrenade(IPlayer thrower, CBaseCSGrenadeProjectile projectile)
    {
        if (!_grenades.TryGetValue(thrower, out var thrownGrenades)) return;

        _trackedCount -= thrownGrenades.RemoveAll(entry => entry.Projectile == projectile);

        if (thrownGrenades.Count == 0)
        {
            _grenades.Remove(thrower);
        }
    }

    internal void Clear()
    {
        _grenades.Clear();
        _players.Clear();
        _trackedCount = 0;
    }
}
