using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Data.Events;

internal sealed class CustomEventsService(ISwiftlyCore core) : ICustomEventService
{
    public void FireFakeDeath(IPlayer attacker, IPlayer victim)
    {
        var attackerMatchStats = attacker.Controller.ActionTrackingServices?.MatchStats;
        if (attackerMatchStats == null)
        {
            return;
        }

        attackerMatchStats.Kills++;
        attackerMatchStats.KillsUpdated();

        attacker.Controller.Score++;
        attacker.Controller.ScoreUpdated();

        var victimMatchStats = victim.Controller.ActionTrackingServices?.MatchStats;
        if (victimMatchStats == null)
        {
            return;
        }

        victimMatchStats.Deaths++;
        victimMatchStats.DeathsUpdated();

        core.GameEvent.FireAsync<EventPlayerDeath>((@event) =>
        {
            @event.UserId = victim.UserID;
            @event.Attacker = attacker.UserID;
            @event.Weapon = "knife";
            @event.Assister = -1;
        });
    }
}
