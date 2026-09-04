using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ElysiumInputProbe.Core;

[PluginMetadata(
    Id = "ElysiumInputProbe.Core",
    Version = "0.4.0",
    Name = "Elysium Input Probe",
    Author = "Elysium",
    Description = "Diagnostic numeric-slot capture with temporary test items and hidden weapon-selection HUD."
)]
public sealed class ElysiumInputProbePluginV4(ISwiftlyCore core) : BasePlugin(core)
{
    // Source HUD flag: HIDEHUD_WEAPONSELECTION = 1 << 0.
    private const uint HideHudWeaponSelection = 1u << 0;

    private static readonly CaptureTestItem[] CaptureTestItems =
    [
        new("5", "slot5", "weapon_c4"),
        new("6", "slot6", "weapon_hegrenade"),
        new("7", "slot7", "weapon_flashbang"),
        new("8", "slot8", "weapon_smokegrenade"),
        new("9", "slot9", "weapon_decoy"),
        new("0", "slot10", "weapon_molotov")
    ];

    private readonly Dictionary<int, ProbeState> _states = [];
    private Guid _commandId = Guid.Empty;

    public override void Load(bool hotReload)
    {
        _commandId = Core.Command.RegisterCommand(
            "inputprobe",
            HandleProbeCommand,
            helpText: "Controls the Elysium numeric input capture probe."
        );

        Core.Event.OnClientDisconnected += OnClientDisconnected;
        Core.GameHooks.Controller.ProcessUsercmds.Pre += OnProcessUsercmds;

        Core.Logger.LogInformation(
            "[InputProbe] v0.4.0 loaded. Use !inputprobe on and !inputprobe capture on."
        );
    }

    public override void Unload()
    {
        foreach (var (playerId, state) in _states.ToArray())
        {
            var player = Core.PlayerManager.GetPlayer(playerId);
            if (player is { IsValid: true })
            {
                StopCapture(player, state, logResult: false);
            }
        }

        Core.GameHooks.Controller.ProcessUsercmds.Pre -= OnProcessUsercmds;
        Core.Event.OnClientDisconnected -= OnClientDisconnected;

        if (_commandId != Guid.Empty)
        {
            Core.Command.UnregisterCommand(_commandId);
            _commandId = Guid.Empty;
        }

        _states.Clear();
    }

    private void HandleProbeCommand(ICommandContext context)
    {
        if (!context.IsSentByPlayer || context.Sender is not { IsValid: true } player)
        {
            context.Reply("ElysiumInputProbe can only be controlled by a player.");
            return;
        }

        var action = context.Args.Length == 0
            ? "status"
            : context.Args[0].Trim().ToLowerInvariant();

        switch (action)
        {
            case "on":
                Enable(player.PlayerID);
                context.Reply("[InputProbe] ON. Use !inputprobe capture on.");
                break;

            case "off":
                Disable(player);
                context.Reply("[InputProbe] OFF.");
                break;

            case "status":
                ReplyStatus(context, player.PlayerID);
                break;

            case "capture":
                SetCapture(context, player);
                break;

            default:
                context.Reply("[InputProbe] Usage: !inputprobe on|off|status|capture on|capture off");
                break;
        }
    }

    private void Enable(int playerId)
    {
        if (_states.ContainsKey(playerId))
        {
            return;
        }

        _states[playerId] = new ProbeState();
        Core.Logger.LogInformation("[InputProbe][CONTROL] player={PlayerId} enabled", playerId);
    }

    private void Disable(IPlayer player)
    {
        if (!_states.TryGetValue(player.PlayerID, out var state))
        {
            return;
        }

        StopCapture(player, state);
        _states.Remove(player.PlayerID);
        Core.Logger.LogInformation("[InputProbe][CONTROL] player={PlayerId} disabled", player.PlayerID);
    }

    private void ReplyStatus(ICommandContext context, int playerId)
    {
        if (!_states.TryGetValue(playerId, out var state))
        {
            context.Reply("[InputProbe] OFF.");
            return;
        }

        context.Reply(
            $"[InputProbe] ON. Capture: {(state.CaptureEnabled ? "on" : "off")}, " +
            $"injected: {state.InjectedWeapons.Count}, hudOwned: {state.OwnsWeaponHudHideBit}."
        );
    }

    private void SetCapture(ICommandContext context, IPlayer player)
    {
        if (context.Args.Length < 2)
        {
            context.Reply("[InputProbe] Usage: !inputprobe capture on|off");
            return;
        }

        if (!_states.TryGetValue(player.PlayerID, out var state))
        {
            state = new ProbeState();
            _states[player.PlayerID] = state;
        }

        switch (context.Args[1].Trim().ToLowerInvariant())
        {
            case "on":
                StartCapture(context, player, state);
                break;

            case "off":
                StopCapture(player, state);
                context.Reply("[InputProbe] Capture OFF. Test items removed and HUD restored.");
                break;

            default:
                context.Reply("[InputProbe] Usage: !inputprobe capture on|off");
                break;
        }
    }

    private void StartCapture(ICommandContext context, IPlayer player, ProbeState state)
    {
        if (state.CaptureEnabled)
        {
            context.Reply("[InputProbe] Capture is already ON.");
            LogInventory(player);
            return;
        }

        var pawn = player.PlayerPawn;
        var itemServices = pawn?.ItemServices;
        var weaponServices = pawn?.WeaponServices;
        if (pawn is not { IsValid: true } || itemServices is null || weaponServices is null)
        {
            context.Reply("[InputProbe] Capture requires an alive player pawn with item/weapon services.");
            return;
        }

        state.CaptureEnabled = true;
        state.InjectedWeapons.Clear();
        state.SeenCommands.Clear();

        HideWeaponHud(player, state, pawn);

        var activeBeforeInjection = weaponServices.ActiveWeapon.IsValid
            ? weaponServices.ActiveWeapon.Value
            : null;

        foreach (var testItem in CaptureTestItems)
        {
            var existing = weaponServices.MyValidWeapons.FirstOrDefault(weapon =>
                weapon.DesignerName.Equals(testItem.DesignerName, StringComparison.OrdinalIgnoreCase)
            );

            if (existing is { IsValid: true })
            {
                Core.Logger.LogInformation(
                    "[InputProbe][INJECT] player={PlayerId} key={Key} slot={Slot} weapon={Weapon} entity={EntityIndex} status=existing",
                    player.PlayerID,
                    testItem.Key,
                    testItem.SlotCommand,
                    existing.DesignerName,
                    existing.Index
                );
                continue;
            }

            try
            {
                var injected = itemServices.GiveItem<CBasePlayerWeapon>(testItem.DesignerName);
                if (injected is not { IsValid: true })
                {
                    Core.Logger.LogWarning(
                        "[InputProbe][INJECT] player={PlayerId} key={Key} slot={Slot} weapon={Weapon} status=failed-invalid",
                        player.PlayerID,
                        testItem.Key,
                        testItem.SlotCommand,
                        testItem.DesignerName
                    );
                    continue;
                }

                state.InjectedWeapons[injected.Index] = injected.DesignerName;
                Core.Logger.LogInformation(
                    "[InputProbe][INJECT] player={PlayerId} key={Key} slot={Slot} weapon={Weapon} entity={EntityIndex} status=added",
                    player.PlayerID,
                    testItem.Key,
                    testItem.SlotCommand,
                    injected.DesignerName,
                    injected.Index
                );
            }
            catch (Exception exception)
            {
                Core.Logger.LogWarning(
                    exception,
                    "[InputProbe][INJECT] player={PlayerId} key={Key} slot={Slot} weapon={Weapon} status=failed-exception",
                    player.PlayerID,
                    testItem.Key,
                    testItem.SlotCommand,
                    testItem.DesignerName
                );
            }
        }

        if (activeBeforeInjection is { IsValid: true }
            && weaponServices.MyValidWeapons.Any(weapon => weapon.Index == activeBeforeInjection.Index))
        {
            weaponServices.SelectWeapon(activeBeforeInjection);
        }

        Core.Logger.LogInformation(
            "[InputProbe][CAPTURE_CONTROL] player={PlayerId} enabled injected={InjectedCount} hideHud={HideHud}",
            player.PlayerID,
            state.InjectedWeapons.Count,
            pawn.HideHUD
        );
        LogInventory(player);

        context.Reply(
            $"[InputProbe] Capture ON. Injected {state.InjectedWeapons.Count} items. Weapon HUD hidden. Test 5,6,7,8,9,0."
        );
    }

    private void HideWeaponHud(IPlayer player, ProbeState state, CCSPlayerPawn pawn)
    {
        var before = pawn.HideHUD;
        state.OwnsWeaponHudHideBit = (before & HideHudWeaponSelection) == 0;

        if (state.OwnsWeaponHudHideBit)
        {
            pawn.HideHUD = before | HideHudWeaponSelection;
            pawn.HideHUDUpdated();
        }

        Core.Logger.LogInformation(
            "[InputProbe][HUD] player={PlayerId} action=hide before={Before} after={After} bit={Bit} owned={Owned}",
            player.PlayerID,
            before,
            pawn.HideHUD,
            HideHudWeaponSelection,
            state.OwnsWeaponHudHideBit
        );
    }

    private void RestoreWeaponHud(IPlayer player, ProbeState state)
    {
        var pawn = player.PlayerPawn;
        if (pawn is not { IsValid: true })
        {
            state.OwnsWeaponHudHideBit = false;
            return;
        }

        var before = pawn.HideHUD;
        if (state.OwnsWeaponHudHideBit)
        {
            pawn.HideHUD = before & ~HideHudWeaponSelection;
            pawn.HideHUDUpdated();
        }

        Core.Logger.LogInformation(
            "[InputProbe][HUD] player={PlayerId} action=restore before={Before} after={After} bit={Bit} owned={Owned}",
            player.PlayerID,
            before,
            pawn.HideHUD,
            HideHudWeaponSelection,
            state.OwnsWeaponHudHideBit
        );

        state.OwnsWeaponHudHideBit = false;
    }

    private void StopCapture(IPlayer player, ProbeState state, bool logResult = true)
    {
        state.CaptureEnabled = false;

        var weaponServices = player.PlayerPawn?.WeaponServices;
        if (weaponServices is not null)
        {
            foreach (var (entityIndex, expectedDesignerName) in state.InjectedWeapons.ToArray())
            {
                var injectedWeapon = weaponServices.MyValidWeapons.FirstOrDefault(weapon =>
                    weapon.Index == entityIndex
                    && weapon.DesignerName.Equals(expectedDesignerName, StringComparison.OrdinalIgnoreCase)
                );

                if (injectedWeapon is not { IsValid: true })
                {
                    continue;
                }

                try
                {
                    weaponServices.RemoveWeapon(injectedWeapon);
                    if (logResult)
                    {
                        Core.Logger.LogInformation(
                            "[InputProbe][CLEANUP] player={PlayerId} weapon={Weapon} entity={EntityIndex} status=removed",
                            player.PlayerID,
                            expectedDesignerName,
                            entityIndex
                        );
                    }
                }
                catch (Exception exception)
                {
                    Core.Logger.LogWarning(
                        exception,
                        "[InputProbe][CLEANUP] player={PlayerId} weapon={Weapon} entity={EntityIndex} status=failed",
                        player.PlayerID,
                        expectedDesignerName,
                        entityIndex
                    );
                }
            }
        }

        state.InjectedWeapons.Clear();
        state.SeenCommands.Clear();
        RestoreWeaponHud(player, state);

        if (logResult)
        {
            Core.Logger.LogInformation("[InputProbe][CAPTURE_CONTROL] player={PlayerId} disabled", player.PlayerID);
            LogInventory(player);
        }
    }

    private void OnProcessUsercmds(ref ProcessUsercmdsPreContext context)
    {
        var player = context.Params.Player;
        if (!_states.TryGetValue(player.PlayerID, out var state) || !state.CaptureEnabled)
        {
            return;
        }

        foreach (var usercmd in context.Params.Usercmds)
        {
            var baseCmd = usercmd.CSGOUserCmd.Base;
            if (baseCmd.Weaponselect == 0)
            {
                continue;
            }

            var isFirstCopy = state.MarkCommandSeen(usercmd.CommandNumber);
            SuppressCaptureSelection(
                player,
                baseCmd,
                usercmd.CommandNumber,
                baseCmd.Weaponselect,
                logSelection: isFirstCopy
            );
        }
    }

    private void SuppressCaptureSelection(
        IPlayer player,
        CBaseUserCmdPB baseCmd,
        uint commandNumber,
        int weaponSelect,
        bool logSelection)
    {
        try
        {
            var candidateIndex = (uint)(weaponSelect & 0x3FFF);
            var entity = Core.EntitySystem.GetEntityByIndex(candidateIndex);
            if (entity is not { IsValid: true }
                || !TryGetCaptureBinding(entity.DesignerName, out var key, out var slotCommand))
            {
                return;
            }

            var activeBefore = DescribeActiveWeapon(player);
            baseCmd.Weaponselect = 0;

            if (logSelection)
            {
                Core.Logger.LogInformation(
                    "[InputProbe][CAPTURE] player={PlayerId} cmd={CommandNumber} key={Key} slot={Slot} weapon={Weapon} entity={EntityIndex} activeBefore={ActiveWeapon} suppressed=True",
                    player.PlayerID,
                    commandNumber,
                    key,
                    slotCommand,
                    entity.DesignerName,
                    candidateIndex,
                    activeBefore
                );
            }
        }
        catch (Exception exception)
        {
            Core.Logger.LogWarning(
                exception,
                "[InputProbe][CAPTURE] player={PlayerId} cmd={CommandNumber} weaponSelect={WeaponSelect} status=failed",
                player.PlayerID,
                commandNumber,
                weaponSelect
            );
        }
    }

    private static bool TryGetCaptureBinding(string designerName, out string key, out string slotCommand)
    {
        foreach (var testItem in CaptureTestItems)
        {
            if (!designerName.Equals(testItem.DesignerName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            key = testItem.Key;
            slotCommand = testItem.SlotCommand;
            return true;
        }

        if (designerName.Equals("weapon_incgrenade", StringComparison.OrdinalIgnoreCase))
        {
            key = "0";
            slotCommand = "slot10";
            return true;
        }

        key = string.Empty;
        slotCommand = string.Empty;
        return false;
    }

    private static string DescribeActiveWeapon(IPlayer player)
    {
        var weaponServices = player.PlayerPawn?.WeaponServices;
        if (weaponServices is null || !weaponServices.ActiveWeapon.IsValid)
        {
            return "-";
        }

        var activeWeapon = weaponServices.ActiveWeapon.Value;
        return activeWeapon is { IsValid: true }
            ? $"{activeWeapon.Index}:{activeWeapon.DesignerName}"
            : "-";
    }

    private void LogInventory(IPlayer player)
    {
        var pawn = player.PlayerPawn;
        var weaponServices = pawn?.WeaponServices;
        if (pawn is not { IsValid: true } || weaponServices is null)
        {
            Core.Logger.LogInformation("[InputProbe][INVENTORY] player={PlayerId} unavailable", player.PlayerID);
            return;
        }

        var active = DescribeActiveWeapon(player);
        var inventory = weaponServices.MyValidWeapons
            .OrderBy(weapon => weapon.Index)
            .Select(weapon => $"{weapon.Index}:{weapon.DesignerName}")
            .ToArray();

        Core.Logger.LogInformation(
            "[InputProbe][INVENTORY] player={PlayerId} hideHud={HideHud} active={ActiveWeapon} weapons=[{Weapons}]",
            player.PlayerID,
            pawn.HideHUD,
            active,
            inventory.Length == 0 ? "-" : string.Join(',', inventory)
        );
    }

    private void OnClientDisconnected(IOnClientDisconnectedEvent @event)
    {
        // При disconnect игровые entities уничтожаются вместе с pawn. Состояние probe просто забываем.
        _states.Remove(@event.PlayerId);
    }

    private sealed class ProbeState
    {
        public bool CaptureEnabled { get; set; }
        public bool OwnsWeaponHudHideBit { get; set; }
        public Dictionary<uint, string> InjectedWeapons { get; } = [];
        public HashSet<uint> SeenCommands { get; } = [];

        public bool MarkCommandSeen(uint commandNumber)
        {
            if (SeenCommands.Count >= 512)
            {
                SeenCommands.Clear();
            }

            return SeenCommands.Add(commandNumber);
        }
    }

    private readonly record struct CaptureTestItem(
        string Key,
        string SlotCommand,
        string DesignerName
    );
}
