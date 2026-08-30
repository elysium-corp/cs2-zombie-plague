using System.Collections.Concurrent;
using CustomEquipment.Api.Events.Contexts.Items;
using SwiftlyS2.Shared.Players;

namespace Economy.Core.Services;

internal sealed class CustomWeaponHitTracker
{
    private const long LifetimeMilliseconds = 1_000;
    private readonly ConcurrentDictionary<ulong, PendingHit> _pendingHits = new();

    public void Track(in WeaponDamageModifiedContext context)
    {
        if (!CanTrack(context.Attacker) || !CanTrack(context.Victim))
        {
            return;
        }

        _pendingHits[context.Attacker.SteamID] = new PendingHit(
            context.Victim.SteamID,
            context.Weapon.InternalName,
            Environment.TickCount64
        );
    }

    public string? Consume(IPlayer attacker, IPlayer victim)
    {
        if (!_pendingHits.TryRemove(attacker.SteamID, out var pendingHit))
        {
            return null;
        }

        if (pendingHit.VictimSteamId != victim.SteamID
            || Environment.TickCount64 - pendingHit.CreatedAtMilliseconds > LifetimeMilliseconds)
        {
            return null;
        }

        return pendingHit.WeaponKey;
    }

    public void Remove(ulong steamId)
    {
        _pendingHits.TryRemove(steamId, out _);

        foreach (var (attackerSteamId, hit) in _pendingHits)
        {
            if (hit.VictimSteamId == steamId)
            {
                _pendingHits.TryRemove(attackerSteamId, out _);
            }
        }
    }

    private static bool CanTrack(IPlayer player)
    {
        return player is { IsValid: true, IsAuthorized: true, IsFakeClient: false }
               && player.SteamID != 0;
    }

    private readonly record struct PendingHit(
        ulong VictimSteamId,
        string WeaponKey,
        long CreatedAtMilliseconds
    );
}
