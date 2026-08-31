using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Items;
using CustomEquipment.Api.Events.Contexts.Mines;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Services;
using CustomEquipment.Utils;
using Localization.Api;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
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
    ILocalizationApi localization)
    : IMineController, IDisposable
{
    private readonly Dictionary<CBaseModelEntity, (IPlayer Owner, LaserMineEntityBase Mine)> _mines = [];

    public void Initialize()
    {
        events.Items.Purchasing.Hook(OnItemPurchasing);
        events.Items.Giving.Hook(OnItemGiving);
        events.Items.Purchased.Hook(OnItemBought);
        events.Mines.Placed.Hook(OnMinePlaced);
        core.GameHooks.Entities.TakeDamage.Pre += OnEntityTakeDamage;
        core.GameHooks.Movement.RunCommand.Pre += OnRunCommand;
        core.GameHooks.Weapons.CanUse.Pre += OnWeaponCanUse;

        var playerEvents = zombiePlagueApi().Events.Players;
        playerEvents.Infected.Hook(OnPlayerInfected);
        playerEvents.BecameNemesis.Hook(OnPlayerBecameNemesis);
    }

    public void Dispose()
    {
        events.Items.Purchasing.Unhook(OnItemPurchasing);
        events.Items.Giving.Unhook(OnItemGiving);
        events.Items.Purchased.Unhook(OnItemBought);
        events.Mines.Placed.Unhook(OnMinePlaced);
        core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;
        core.GameHooks.Movement.RunCommand.Pre -= OnRunCommand;
        core.GameHooks.Weapons.CanUse.Pre -= OnWeaponCanUse;

        var playerEvents = zombiePlagueApi().Events.Players;
        playerEvents.Infected.Unhook(OnPlayerInfected);
        playerEvents.BecameNemesis.Unhook(OnPlayerBecameNemesis);

        foreach (var mine in _mines.Values) mine.Mine.Dispose();
        _mines.Clear();
    }

    private void OnItemPurchasing(ref ItemPurchasingContext context)
    {
        if (context.Item is not LaserMine)
        {
            return;
        }

        var player = context.Player;

        if (!HasLaserMine(player))
        {
            return;
        }

        player.SendAlert(localization.GetForPlayerOrKey(player, "Equipment.LaserMine.AlreadyOwned"));
        context.Cancel();
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

    private void OnItemBought(ref ItemPurchasedContext context)
    {
        if (context.Item is not LaserMine)
        {
            return;
        }

        context.Player.SendAlert(
            localization.GetForPlayerOrKey(context.Player, "Equipment.LaserMine.Granted"));
    }

    private void OnMinePlaced(ref MinePlacedContext context)
    {
        equipmentService.RemoveItems<LaserMine>(context.Player);

        var entity = context.Mine.LaserMine;

        if (entity == null) return;

        _mines[entity] = (context.Player, context.Mine);
    }

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
