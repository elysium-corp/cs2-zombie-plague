using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Data.Events;

public class CustomEventsService(ISwiftlyCore core) : ICustomEventService
{
    public void ShowInfection(IPlayer? attacker, IPlayer? victim)
    {
        if (attacker != null)
        {
            var matchStats = attacker.Controller.ActionTrackingServices?.MatchStats;
            
            if (matchStats == null)
            {
                return;
            }
            
            matchStats.Kills++;
            matchStats.KillsUpdated();
            
            attacker.Controller.Score++;
            attacker.Controller.ScoreUpdated();
        }

        if (victim != null && attacker != null)
        {
            var matchStats = victim.Controller.ActionTrackingServices?.MatchStats;
            
            if (matchStats == null)
            {
                return;
            }
            
            matchStats.Deaths++;
            matchStats.DeathsUpdated();
            
            core.GameEvent.FireAsync<EventPlayerDeath>((@event) =>
            {
                @event.UserId = victim.UserID;
                @event.Attacker = attacker.UserID;
                @event.Weapon = "biohazard";
                @event.Assister = -1;
            });
        }
    }
}