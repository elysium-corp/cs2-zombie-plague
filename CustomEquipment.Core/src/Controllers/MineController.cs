using System;
using System.Collections.Generic;
using System.Linq;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Items;
using CustomEquipment.Api.Events.Contexts.Mines;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Services;
using CustomEquipment.Utils;
using Localization.Api;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Api;
using ZombiePlague.Api.Events.Contexts.Player;

namespace CustomEquipment.Controllers;

internal sealed class MineController(
    ISwiftlyCore core,
    ICustomEquipmentEvents events,
    IEquipmentService equipmentService,
    ILaserMineInstallerService laserMineInstallerService,
    Func<IZombiePlagueApi> zombiePlagueApi,
    ILocalizationApi localization,
    GameplayItemCatalog gameplayItemCatalog)
    : IMineController, IDisposable
{
    private readonly Dictionary<CBaseModelEntity, (IPlayer Owner, LaserMineEntityBase Mine)> _mines = [];
    private Guid _roundEndHook = Guid.Empty;
    private Guid _gameRestartHook = Guid.Empty;
    private Guid _playerDisconnectHook = Guid.Empty;
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        events.Items.Giving.Hook(OnItemGiving);
        events.Items.Given.Hook(OnItemGiven);
        events.Mines.Placed.Hook(OnMinePlaced);
        core.Event.OnMapLoad += OnMapLoad;
        _roundEndHook = core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        _gameRestartHook = core.GameEvent.HookPost<EventCsPreRestart>(OnGameRestart);
        _playerDisconnectHook = core.GameEvent.HookPost<EventPlayerDisconnect>(OnPlayerDisconnect);
        core.GameHooks.Movement.RunCommand.Pre += OnRunCommand;
        core.GameHooks.Weapons.CanUse.Pre += OnWeaponCanUse;
        core.GameHooks.Entities.TakeDamage.Pre += OnEntityTakeDamage;

        var playerEvents = zombiePlagueApi().Events.Players;
        playerEvents.Infected.Hook(OnPlayerInfected);
        playerEvents.BecameNemesis.Hook(OnPlayerBecameNemesis);
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        events.Items.Giving.Unhook(OnItemGiving);
        events.Items.Given.Unhook(OnItemGiven);
        events.Mines.Placed.Unhook(OnMinePlaced);
        core.Event.OnMapLoad -= OnMapLoad;
        core.GameEvent.Unhook(_roundEndHook);
        core.GameEvent.Unhook(_gameRestartHook);
        core.GameEvent.Unhook(_playerDisconnectHook);
        _roundEndHook = Guid.Empty;
        _gameRestartHook = Guid.Empty;
        _playerDisconnectHook = Guid.Empty;
        core.GameHooks.Movement.RunCommand.Pre -= OnRunCommand;
        core.GameHooks.Weapons.CanUse.Pre -= OnWeaponCanUse;
        core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;

        var playerEvents = zombiePlagueApi().Events.Players;
        playerEvents.Infected.Unhook(OnPlayerInfected);
        playerEvents.BecameNemesis.Unhook(OnPlayerBecameNemesis);

        RemoveAllMines();
    }

    private void OnItemGiving(ref ItemGivingContext context)
    {
        if (context.Item is not LaserMine || !HasLaserMine(context.Player))
        {
            return;
        }

        context.Player.SendAlert(
            localization.GetForPlayerOrKey(context.Player, "Equipment.LaserMine.AlreadyOwned"));
        context.Cancel();
    }

    private void OnItemGiven(ref ItemGivenContext context)
    {
        if (context.Item is not LaserMine)
        {
            return;
        }

        context.Player.SendAlert(
            localization.GetForPlayerOrKey(context.Player, "Equipment.LaserMine.Granted"));
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        RemoveAllMines();
        return HookResult.Continue;
    }

    private void OnEntityTakeDamage(ref TakeDamageEntityPreContext hook)
    {
        var attacker = hook.Params.Info.Attacker.ResolvePlayerFromHandle();
        var victim = hook.Params.Entity as CBaseModelEntity;

        if (victim == null || attacker == null) return;
        if (!_mines.TryGetValue(victim, out var entry)) return;
        if (attacker.PlayerPawn?.Team != victim.Team) return;

        if (!attacker.Equals(entry.Owner))
        {
            hook.Params.Info.Damage = 0;
            return;
        }

        if (victim.Health - hook.Params.Info.Damage <= 0)
        {
            _mines.Remove(victim);
            entry.Mine.Dispose();
        }
    }

    private HookResult OnGameRestart(EventCsPreRestart @event)
    {
        RemoveAllMines();
        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        if (@event.UserIdPlayer is { } player)
        {
            RemovePlayerMines(player);
        }

        return HookResult.Continue;
    }

    private void OnMinePlaced(ref MinePlacedContext context)
    {
        var entity = context.Mine.LaserMine;
        if (entity is not { IsValidEntity: true }) return;
        _mines[entity] = (context.Player, context.Mine);
        equipmentService.RemoveItems<LaserMine>(context.Player);
    }

    private void OnMapLoad(IOnMapLoadEvent @event) => RemoveAllMines();

    private void OnRunCommand(ref RunCommandMovementPreContext context)
    {
        var buttons = context.Params.UserCmd.ButtonState;
        var player = context.Params.Player;
        var laserMine = equipmentService.GetActiveItem<LaserMine>(player);

        if (laserMine is null)
        {
            return;
        }

        // C4 используется только как предмет-носитель лазерной мины:
        // стандартная установка бомбы блокируется.
        buttons.ButtonPressed &= ~GameButtonFlags.Mouse1;
        buttons.ButtonChanged &= ~GameButtonFlags.Mouse1;

        var isSecondaryAttackPressed =
            (buttons.ButtonPressed & GameButtonFlags.Mouse2) != 0 &&
            (buttons.ButtonChanged & GameButtonFlags.Mouse2) != 0;

        if (!isSecondaryAttackPressed)
        {
            return;
        }

        buttons.ButtonPressed &= ~GameButtonFlags.Mouse2;
        buttons.ButtonChanged &= ~GameButtonFlags.Mouse2;

        if (!laserMineInstallerService.TrySetup(player, laserMine))
        {
            player.SendAlert(localization.GetForPlayerOrKey(player, "Equipment.LaserMine.InvalidSurface"));
        }
    }

    private void OnWeaponCanUse(ref CanUseWeaponPreContext context)
    {
        var player = context.Params.Player;

        if (equipmentService.GetItemByEntityIndex<LaserMine>(context.Params.Weapon.Index) is null)
        {
            return;
        }

        UpdateNotValidMines();

        if (!_mines.Values.Any(entry => entry.Owner.Equals(player)))
        {
            return;
        }

        context.SetReturn(false);
        context.SetHookResult(HookResult.Stop);
    }

    private void UpdateNotValidMines()
    {
        foreach (var pair in _mines.ToArray())
        {
            if (!pair.Key.IsValidEntity)
            {
                _mines.Remove(pair.Key);
                pair.Value.Mine.Dispose();
            }
        }
    }

    private bool HasLaserMine(IPlayer player)
    {
        UpdateNotValidMines();

        return equipmentService.HasItem<LaserMine>(player) ||
               _mines.Values.Any(entry => entry.Owner.Equals(player));
    }

    private void OnPlayerInfected(ref PlayerInfectedContext context)
    {
        RemovePlayerMines(context.Player);
    }

    private void OnPlayerBecameNemesis(ref PlayerBecameNemesisContext context)
    {
        RemovePlayerMines(context.Player);
    }

    private void RemoveAllMines()
    {
        // Незавершённая установка не должна перенести spawn мины через границу раунда.
        laserMineInstallerService.CancelAll();

        // Сначала убираем native entities из lookup-карты контроллера, затем despawn.
        // Это не даёт damage hooks повторно получить мину во время её удаления.
        var mines = _mines.Values
            .Select(entry => entry.Mine)
            .Distinct()
            .ToArray();

        _mines.Clear();

        foreach (var mine in mines)
        {
            mine.Dispose();
        }
    }

    private void RemovePlayerMines(IPlayer player)
    {
        laserMineInstallerService.Cancel(player);

        foreach (var pair in _mines
                     .Where(pair => pair.Value.Owner.Equals(player))
                     .ToArray())
        {
            _mines.Remove(pair.Key);
            pair.Value.Mine.Dispose();
        }
    }
}