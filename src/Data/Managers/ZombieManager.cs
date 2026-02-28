using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Events;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Zombies;
using CS2ZombiePlague.Data.Zombies.ZClasses;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Managers;

public class ZombieManager(ISwiftlyCore core, HumanManager humanManager, IZombieFactory zombieFactory, ZClassMenu zClassMenu, IEventPublisher eventPublisher)
{
    private readonly Dictionary<int, Zombie> _zombiePlayers = new();
    
    private Guid _onPlayerDisconnectEvent;
    
    public void RegisterHooks()
    {
        _onPlayerDisconnectEvent = core.GameEvent.HookPost<EventPlayerDisconnect>(OnPlayerDisconnect);
        core.Event.OnWeaponServicesCanUseHook += OnItemServicesCanAcquireHook;
        core.Event.OnWeaponServicesDropWeaponHook += OnWeaponServicesDropWeaponHook;
    }
    
    private void OnWeaponServicesDropWeaponHook(IOnWeaponServicesDropWeaponHook @event)
    {
        var pawn = @event.WeaponServices.Pawn;
        var player = core.PlayerManager.GetPlayerFromPawn(pawn);

        if (player == null || !player.IsValid)
        {
            return;
        }

        if (player.IsInfected())
        {
            @event.Result = HookResult.Stop;
        }
    }
    
    private void OnItemServicesCanAcquireHook(IOnWeaponServicesCanUseHookEvent @event)
    {
        var pawn = @event.WeaponServices.Pawn;
        var player = core.PlayerManager.GetPlayerFromPawn(pawn);

        if (player == null || !player.IsValid)
        {
            return;
        }
            
        var weaponName = @event.Weapon.DesignerName;

        if (player.IsInfected() && !weaponName.Contains("knife") && !weaponName.Contains("smoke"))
        {
            @event.SetResult(false);
        }
    }
    
    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var playerId = @event.PlayerID;
        var zombie = GetZombie(playerId);
        if (zombie != null)
        {
            _zombiePlayers.Remove(playerId);
        }
        
        return HookResult.Continue;
    }
    
    public Zombie? CreateZombie(IPlayer player, IPlayer? infector = null)
    {
        if (!player.IsValid)
        {
            return null;
        }

        if (infector != null)
        {
            FireFakeDeath(infector.PlayerID, player.PlayerID);
            eventPublisher.OnPlayerInfectedBy(infector, player);
        }
        
        eventPublisher.OnPlayerInfected(player);

        var zClass = GetZClassFromMenu(player.PlayerID);
        return _zombiePlayers[player.PlayerID] =
            zombieFactory.Create(core, this, player, zClass);
    }
    
    public void SetNemesis(IPlayer player, INemesisConfig? roundConfig = null)
    {
        var playerPawn = player.PlayerPawn;
        if ( playerPawn == null || !player.IsAlive)
        {
            return;
        }
        
        eventPublisher.OnPlayerInfected(player);
        
        var nemesis = DependencyManager.GetService<ZNemesis>();
        _zombiePlayers[player.PlayerID] = zombieFactory.Create(core, this, player, nemesis, true);
        
        var countPlayers = humanManager.GetAllHumanPlayers().Count;

        if (roundConfig == null)
        {
            return;
        }
        
        core.Scheduler.NextTick(() =>
        {
            player.SetHealth(playerPawn.Health + (roundConfig.NemesisBonusHealthPerPlayer * countPlayers));
        });
    }
    
    public void Respawn(IPlayer player)
    {
        if (player.IsAlive)
        {
            return;
        }

        var zombie = player.IsInfected() ? GetZombie(player.PlayerID) : CreateZombie(player);

        if (zombie == null)
        {
            return;
        }
        
        player.SwitchTeam(Team.T);
        player.Respawn();
        zombie.Initialize();
    }

    public void Remove(IPlayer player)
    {
        _zombiePlayers[player.PlayerID].UnHookAbilities();
        _zombiePlayers.Remove(player.PlayerID);
    }

    public void RemoveAll()
    {
        foreach (var zPlayer in _zombiePlayers.Values)
        {
            zPlayer.UnHookAbilities();
        }

        _zombiePlayers.Clear();
    }

    public Zombie? GetZombie(int playerId)
    {
        return _zombiePlayers.GetValueOrDefault(playerId);
    }

    public Dictionary<int, Zombie> GetAllZombies()
    {
        return _zombiePlayers;
    }

    public IZClass GetZClassFromMenu(int playerId)
    {
        return zClassMenu.GetPlayerZClass(playerId);
    }

    private void FireFakeDeath(int infectorId, int victimId)
    {
        var infector = core.PlayerManager.GetPlayer(infectorId);
        var victim = core.PlayerManager.GetPlayer(victimId);

        if (infector != null)
        {
            var matchStats = infector.Controller.ActionTrackingServices?.MatchStats;
            if (matchStats == null)
            {
                return;
            }
            
            matchStats.Kills++;
            matchStats.KillsUpdated();
            
            infector.Controller.Score++;
            infector.Controller.ScoreUpdated();
        }

        if (victim != null)
        {
            var matchStats = victim.Controller.ActionTrackingServices?.MatchStats;
            if (matchStats == null)
            {
                return;
            }
            
            matchStats.Deaths++;
            matchStats.DeathsUpdated();
        }

        core.GameEvent.FireAsync<EventPlayerDeath>((@event) =>
        {
            @event.UserId = victimId;
            @event.Attacker = infectorId;
            @event.Weapon = "knife";
            @event.Assister = -1;
        });
    }
}