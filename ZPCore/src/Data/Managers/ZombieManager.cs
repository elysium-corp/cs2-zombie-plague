using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZPApi.Events;
using ZPCore.Config.Round;
using ZPCore.Data.Extensions;
using ZPCore.Data.Zombies;
using ZPCore.Data.Zombies.ZClasses;
using ZPCore.Di;

namespace ZPCore.Data.Managers;

internal class ZombieManager(ISwiftlyCore core, HumanManager humanManager, IZombieFactory zombieFactory, ZClassMenu zClassMenu, IEventPublisher eventPublisher)
{
    private readonly Dictionary<int, Zombie> _zombies = new();
    
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
            _zombies.Remove(playerId);
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
        return _zombies[player.PlayerID] =
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
        _zombies[player.PlayerID] = zombieFactory.Create(core, this, player, nemesis, true);
        
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

    public bool IsNemesis(IPlayer player)
    {
        if (_zombies.TryGetValue(player.PlayerID, out var zombie))
        {
            return zombie.IsNemesis;
        }

        return false;
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
        _zombies[player.PlayerID].UnHookAbilities();
        _zombies[player.PlayerID].SoundController?.Dispose();
        _zombies.Remove(player.PlayerID);
    }

    public void RemoveAll()
    {
        foreach (var zPlayer in _zombies.Values)
        {
            zPlayer.UnHookAbilities();
            zPlayer.SoundController?.Dispose();
        }

        _zombies.Clear();
    }

    public Zombie? GetZombie(int playerId)
    {
        return _zombies.GetValueOrDefault(playerId);
    }

    public Dictionary<int, Zombie> GetAllZombies()
    {
        return _zombies;
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