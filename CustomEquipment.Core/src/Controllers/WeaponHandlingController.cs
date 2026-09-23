using CustomEquipment.Api.Data;
using CustomEquipment.Services;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Controllers;

internal sealed class WeaponHandlingController(
    ISwiftlyCore core,
    IEquipmentService equipmentService
) : IWeaponHandlingController, IDisposable
{
    private readonly WeaponHandlingPrediction _prediction = new(core.ConVar);
    private Guid _weaponFireHook = Guid.Empty;
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        core.GameHooks.Movement.RunCommand.Pre += OnRunCommandPre;
        core.GameHooks.Movement.RunCommand.Post += OnRunCommandPost;
        _weaponFireHook = core.GameEvent.HookPre<EventWeaponFire>(OnWeaponFire);
        core.Event.OnTick += OnTick;
        core.Event.OnClientDisconnected += OnClientDisconnected;
        core.Event.OnMapLoad += OnMapLoad;
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        core.GameHooks.Movement.RunCommand.Pre -= OnRunCommandPre;
        core.GameHooks.Movement.RunCommand.Post -= OnRunCommandPost;
        core.GameEvent.Unhook(_weaponFireHook);
        _weaponFireHook = Guid.Empty;
        core.Event.OnTick -= OnTick;
        core.Event.OnClientDisconnected -= OnClientDisconnected;
        core.Event.OnMapLoad -= OnMapLoad;
        _prediction.Restore(core.PlayerManager.GetAllValidPlayers());
    }

    private void OnRunCommandPre(ref RunCommandMovementPreContext context) => UpdatePlayer(context.Params.Player);

    private void OnRunCommandPost(ref RunCommandMovementPostContext context) => UpdatePlayer(context.Params.Player);

    private HookResult OnWeaponFire(EventWeaponFire @event)
    {
        if (@event.UserIdPlayer is { } player)
        {
            // В одной обработке команд возможны несколько выстрелов.
            UpdatePlayer(player);
        }

        return HookResult.Continue;
    }

    private void OnTick()
    {
        foreach (var player in core.PlayerManager.GetAllValidPlayers())
        {
            UpdatePlayer(player);
        }
    }

    private void OnClientDisconnected(IOnClientDisconnectedEvent @event) => _prediction.Remove(@event.PlayerId);

    private void OnMapLoad(IOnMapLoadEvent @event) =>
        _prediction.Restore(core.PlayerManager.GetAllValidPlayers());

    private void UpdatePlayer(IPlayer player)
    {
        if (!player.IsValid)
        {
            return;
        }

        var pawn = player.PlayerPawn;
        var item = player.IsAlive && pawn is { IsValid: true }
            ? equipmentService.GetActiveItem<WeaponItemBase>(player)
            : null;

        if (item?.AttachedWeapon is not { IsValid: true } weapon)
        {
            _prediction.Update(player, false, false);
            return;
        }

        var (noRecoil, noSpread) = WeaponHandlingRuntime.GetOverrides(
            item.WeaponRecoil, item.WeaponAccuracy, (int)weapon.WeaponMode);

        if (noRecoil && pawn?.AimPunchServices is { } aimPunch)
        {
            WeaponHandlingRuntime.ResetRecoil(aimPunch);
        }

        if (noSpread)
        {
            WeaponHandlingRuntime.ResetAccuracy(weapon);
        }

        _prediction.Update(player, noRecoil, noSpread);
    }
}
