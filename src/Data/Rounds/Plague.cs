using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Rounds;

public class Plague(
    ISwiftlyCore core,
    RoundManager roundManager,
    ZombieManager zombieManager,
    CommonUtils commonUtils,
    PlagueConfig config) : IRound
{
    private Guid _playerDeathEvent = Guid.Empty;
    private Guid _playerConnect = Guid.Empty;
    
    public void End()
    {
        core.Event.OnEntityTakeDamage -= TakeDamage;

        if (config.ZombieRevived)
        {
            core.GameEvent.Unhook(_playerDeathEvent);
            core.GameEvent.Unhook(_playerConnect);
        }

        roundManager.SetRound(new None());

        core.PlayerManager.SendCenter("Раунд окончен");
    }

    public void Start()
    {
        core.Event.OnEntityTakeDamage += TakeDamage;

        if (config.ZombieRevived)
        {
            _playerDeathEvent = core.GameEvent.HookPre<EventPlayerDeath>(EventPlayerDeath);
            _playerConnect = core.GameEvent.HookPre<EventPlayerConnectFull>(EventPlayerConnect);
        }

        var players = core.PlayerManager.GetAlive().ToList();
        var countZombies = Math.Ceiling(players.Count * config.ZombieSpawnRatio);
        var newPlayers = players.Shuffle();

        foreach (var player in newPlayers)
        {
            if (player != null && player.IsValid)
            {
                zombieManager.CreateZombie(player);
                countZombies--;
            }

            if (countZombies == 0)
            {
                break;
            }
        }

        foreach (var player in players)
        {
            if (!player.IsInfected())
            {
                player.SwitchTeam(Team.CT);
            }
        }

        core.PlayerManager.SendCenter("Массовое заражение!");
    }

    private void TakeDamage(IOnEntityTakeDamageEvent @event)
    {
        var attacker = commonUtils.ResolvePlayerFromHandle(@event.Info.Attacker);
        var victim = commonUtils.FindPlayerByPawnAddress(@event.Entity.Address);
        
        if (victim == null || !victim.IsValid || attacker == null)
            return;

        if (!attacker.IsInfected() || victim.IsLastHuman())
        {
            return;
        }
        
        var zombie = zombieManager.GetZombie(attacker.PlayerID);
        
        if (!zombie.Infect(victim))
        {
            if (!victim.IsLastHuman())
            {
                victim.SetArmor(victim.PlayerPawn.ArmorValue - (int)@event.Info.Damage);
            }
        }

        @event.Info.Damage = 0;
    }

    private HookResult EventPlayerDeath(EventPlayerDeath @event)
    {
        var player = @event.UserIdPlayer;
        core.Scheduler.DelayBySeconds(config.ZombieSpawnTime, () =>
        {
            if (player is not { IsValid: true } || roundManager.GetRound() != this)
            {
                return;
            }
            
            if (player.IsInfected())
            {
                player.Controller.Respawn();
                var zombie = zombieManager.GetZombie(player.PlayerID);
                zombie.Initialize();
            }
            else
            {
                player.Controller.Respawn();
                var zombie = zombieManager.CreateZombie(player);
            }
        });

        return HookResult.Continue;
    }
    
    private HookResult EventPlayerConnect(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;
        core.Scheduler.DelayBySeconds(config.ZombieSpawnTime, () =>
        {
            if (player is { IsValid: true } && player.IsInfected() && roundManager.GetRound() == this)
            {
                player.Controller.Respawn();
                if (zombieManager.GetZombie(player.PlayerID) != null)
                {
                    var zombie = zombieManager.GetZombie(player.PlayerID);
                    zombie.Initialize();
                }
                else
                {
                    zombieManager.CreateZombie(player);
                }
                
            }
        });

        return HookResult.Continue;
    }
}