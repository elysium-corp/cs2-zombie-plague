using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Events;
using ZombiePlague.Core.Data.Menus;
using ZombiePlague.Core.Data.Zombies;
using ZombiePlague.Core.Data.Zombies.ZClasses;
using ZombiePlague.Core.Di;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Managers;

internal sealed class ZombieManager(
    ISwiftlyCore core,
    HumanManager humanManager,
    IZombieFactory zombieFactory,
    ZClassMenu zClassMenu,
    ICustomEventService customEventService,
    IEventPublisher eventPublisher)
{
    private readonly Dictionary<int, Zombie> _zombies = new();

    public void RegisterHooks()
    {
        core.GameEvent.HookPost<EventPlayerDisconnect>(OnPlayerDisconnect);
        core.Event.OnWeaponServicesCanUseHook += OnItemServicesCanAcquireHook;
        core.Event.OnWeaponServicesDropWeaponHook += OnWeaponServicesDropWeaponHook;
        zClassMenu.RegisterMenu();
    }

    private void OnWeaponServicesDropWeaponHook(IOnWeaponServicesDropWeaponHook @event)
    {
        var player = core.PlayerManager.GetPlayerFromPawn(@event.WeaponServices.Pawn);

        if (player?.IsValid != true)
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
        var player = core.PlayerManager.GetPlayerFromPawn(@event.WeaponServices.Pawn);

        if (player?.IsValid != true)
        {
            return;
        }

        if (!player.IsInfected())
        {
            return;
        }

        var weaponName = @event.Weapon.DesignerName;

        if (player.IsInfected() && !weaponName.Contains("knife")
                                && !weaponName.Contains("smoke"))
        {
            @event.SetResult(false);
        }
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var playerId = @event.PlayerID;

        _zombies.Remove(playerId);

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
            customEventService.FireFakeDeath(infector, player);
            eventPublisher.OnPlayerInfectedBy(infector, player);
            SoundExt.PlayAt(player, "ZombiePlagueSounds.zombie_transformation_1", 1);
        }

        eventPublisher.OnPlayerInfected(player);

        var zombie = zombieFactory.Create(core, this, player, GetZClassFromMenu(player));
        _zombies[player.PlayerID] = zombie;

        return zombie;
    }

    public void SetNemesis(IPlayer player, INemesisConfig? roundConfig = null)
    {
        var playerPawn = player.PlayerPawn;
        if (playerPawn == null || !player.IsAlive)
        {
            return;
        }

        eventPublisher.OnPlayerInfected(player);

        var nemesis = DependencyManager.GetService<ZNemesis>();
        
        _zombies[player.PlayerID] = zombieFactory.Create(core, this, player, nemesis, true);

        if (roundConfig == null)
        {
            return;
        }

        var bonusHealth = roundConfig.NemesisBonusHealthPerPlayer * humanManager.GetAllHumanPlayers().Count;

        core.Scheduler.NextTick(() =>
        {
            player.SetHealth(playerPawn.Health + bonusHealth);
        });
    }

    public bool IsNemesis(IPlayer player)
    {
        return _zombies.TryGetValue(player.PlayerID, out var z) && z.IsNemesis;
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
        if (_zombies.TryGetValue(player.PlayerID, out var zombie))
        {
            zombie.UnHookAbilities();
            _zombies[player.PlayerID].SoundController?.Dispose();
            _zombies.Remove(player.PlayerID);
        }
    }

    public void RemoveAll()
    {
        foreach (var zombie in _zombies.Values)
        {
            zombie .UnHookAbilities();
            zombie.SoundController?.Dispose();
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

    public IZClass GetZClassFromMenu(IPlayer player)
    {
        return zClassMenu.GetPlayerZClass(player);
    }
}