using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Events;
using ZombiePlague.Core.Data.Menus.Contracts;
using ZombiePlague.Core.Data.Zombies;
using ZombiePlague.Core.Data.Zombies.ZClasses;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Managers;

internal sealed class ZombieManager(
    ISwiftlyCore core,
    IHumanManager humanManager,
    IZombieFactory zombieFactory,
    IZClassMenu zClassMenu,
    ICustomEventService customEventService,
    IEventPublisher eventPublisher) : IZombieManager
{
    private readonly Dictionary<int, IZombie> _zombies = [];
    private Guid _onPlayerDisconnectEvent;
    private bool _hooksRegistered;

    public void RegisterHooks()
    {
        if (_hooksRegistered)
        {
            return;
        }

        _onPlayerDisconnectEvent = core.GameEvent.HookPost<EventPlayerDisconnect>(OnPlayerDisconnect);
        core.Event.OnWeaponServicesCanUseHook += OnItemServicesCanAcquireHook;
        core.Event.OnWeaponServicesDropWeaponHook += OnWeaponServicesDropWeaponHook;
        zClassMenu.RegisterMenu();
        _hooksRegistered = true;
    }

    public void UnregisterHooks()
    {
        if (!_hooksRegistered)
        {
            return;
        }

        core.GameEvent.Unhook(_onPlayerDisconnectEvent);
        core.Event.OnWeaponServicesCanUseHook -= OnItemServicesCanAcquireHook;
        core.Event.OnWeaponServicesDropWeaponHook -= OnWeaponServicesDropWeaponHook;
        RemoveAll();
        zClassMenu.Clear();
        _hooksRegistered = false;
    }

    private void OnWeaponServicesDropWeaponHook(IOnWeaponServicesDropWeaponHook @event)
    {
        var player = core.PlayerManager.GetPlayerFromPawn(@event.WeaponServices.Pawn);

        if (player?.IsValid != true)
        {
            return;
        }

        if (GetZombie(player) != null)
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

        if (GetZombie(player) == null)
        {
            return;
        }

        var weaponName = @event.Weapon.DesignerName;

        if (!weaponName.Contains("knife") && !weaponName.Contains("smoke"))
        {
            @event.SetResult(false);
        }
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var playerId = @event.PlayerID;

        if (_zombies.Remove(playerId, out var zombie))
        {
            zombie.Dispose();
        }

        zClassMenu.RemovePlayer(playerId);

        return HookResult.Continue;
    }

    public IZombie? CreateZombie(IPlayer player, IPlayer? infector = null)
    {
        if (!player.IsValid)
        {
            return null;
        }

        var zombie = zombieFactory.Create(player, GetZClassFromMenu(player));
        StoreZombie(zombie);

        if (infector != null)
        {
            customEventService.FireFakeDeath(infector, player);
            eventPublisher.OnPlayerInfectedBy(infector, player);
            SoundExt.PlayAt(player, "ZombiePlagueSounds.zombie_transformation_1", 1);
        }

        eventPublisher.OnPlayerInfected(player);
        core.Scheduler.NextWorldUpdate(zombie.Initialize);

        return zombie;
    }

    public bool IsNemesis(IPlayer player)
    {
        return GetZombie(player)?.IsNemesis == true;
    }

    public void SetNemesis(IPlayer player, INemesisConfig? roundConfig = null)
    {
        var playerPawn = player.PlayerPawn;
        if (playerPawn == null || !player.IsAlive)
        {
            return;
        }

        var zombie = zombieFactory.Create<ZNemesis>(player, true);
        StoreZombie(zombie);
        eventPublisher.OnPlayerInfected(player);
        core.Scheduler.NextWorldUpdate(zombie.Initialize);

        if (roundConfig == null)
        {
            return;
        }

        var bonusHealth = roundConfig.NemesisBonusHealthPerPlayer * humanManager.GetHumanCount();

        core.Scheduler.NextTick(() =>
        {
            player.SetHealth(playerPawn.Health + bonusHealth);
        });
    }

    public void Respawn(IPlayer player)
    {
        if (player.IsAlive)
        {
            return;
        }

        var zombie = GetZombie(player);
        var createdNow = false;

        if (zombie == null)
        {
            zombie = CreateZombie(player);
            createdNow = true;
        }
        else if (!zombie.IsNemesis)
        {
            var selectedZClass = GetZClassFromMenu(player);
            if (!ReferenceEquals(zombie.ZClass, selectedZClass))
            {
                zombie = zombieFactory.Create(player, selectedZClass);
                StoreZombie(zombie);
            }
        }

        if (zombie == null)
        {
            return;
        }

        player.SwitchTeam(Team.T);
        player.Respawn();

        if (!createdNow)
        {
            core.Scheduler.NextWorldUpdate(zombie.Initialize);
        }
    }

    public void RemoveAll()
    {
        foreach (var zombie in _zombies.Values)
        {
            zombie.Dispose();
        }

        _zombies.Clear();
    }

    public IZombie? GetZombie(IPlayer player)
    {
        return _zombies.GetValueOrDefault(player.PlayerID);
    }

    public IReadOnlyDictionary<int, IZombie> GetAllZombies()
    {
        return _zombies;
    }

    private IZClass GetZClassFromMenu(IPlayer player)
    {
        return zClassMenu.GetPlayerZClass(player);
    }

    private void StoreZombie(IZombie zombie)
    {
        if (_zombies.Remove(zombie.Player.PlayerID, out var previous))
        {
            previous.Dispose();
        }

        _zombies[zombie.Player.PlayerID] = zombie;
    }
}
