using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;

namespace CS2ZombiePlague.Data.Rounds;

public class Infection(
    ISwiftlyCore core,
    RoundManager roundManager,
    ZombieManager zombieManager,
    CommonUtils commonUtils,
    InfectionConfig config) : IRound
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
        IPlayer firstZombie;

        if (zombieManager.GetAllZombies().Any())
        {
            firstZombie = zombieManager.GetAllZombies().First().Value.GetPlayer();
        }
        else
        {
            firstZombie = players[Random.Shared.Next(0, players.Count)];
            zombieManager.CreateZombie(firstZombie);
        }

        PlaySound(firstZombie);

        firstZombie.SetHealth((int)(firstZombie.PlayerPawn!.Health * config.FirstZombieHealthRatio));

        foreach (var player in players)
        {
            if (!player.IsInfected())
            {
                player.SwitchTeam(Team.CT);
            }
        }

        core.PlayerManager.SendCenter("Первый заражённый => " + firstZombie.Controller.PlayerName);
    }

    private void TakeDamage(IOnEntityTakeDamageEvent @event)
    {
        var attacker = commonUtils.ResolvePlayerFromHandle(@event.Info.Attacker);
        var victim = commonUtils.FindPlayerByPawnAddress(@event.Entity.Address);
        if (victim == null || !victim.IsValid || attacker == null)
            return;
        if (attacker.IsInfected())
        {
            var zombie = zombieManager.GetZombie(attacker.PlayerID);
            if (!zombie.Infect(victim))
            {
                if (!victim.IsLastHuman())
                {
                    victim.SetArmor(victim.PlayerPawn.ArmorValue - (int)@event.Info.Damage);
                    @event.Info.Damage = 0;
                }
            }
        }
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
                    zombie?.Initialize();
                }
                else
                {
                    zombieManager.CreateZombie(player);
                }
            }
        });

        return HookResult.Continue;
    }

    private bool PlaySound(IPlayer zombie)
    {
        using var soundEvent = new SoundEvent()
        {
            Volume = 2,
            Name = "ZombiePlagueAbility.Infection",
            SourceEntityIndex = (int)zombie.RequiredPlayerPawn.Index
        };
        soundEvent.Recipients.AddAllPlayers();
        soundEvent.Emit();

        return true;
    }
}