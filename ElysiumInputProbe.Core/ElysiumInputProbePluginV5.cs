using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ElysiumInputProbe.Core;

[PluginMetadata(
    Id = "ElysiumInputProbe.Core",
    Version = "0.5.0",
    Name = "Elysium Input Probe",
    Author = "Elysium",
    Description = "Diagnostic numeric-slot capture with isolated item injection and robust cleanup."
)]
public sealed class ElysiumInputProbePluginV5(ISwiftlyCore core) : BasePlugin(core)
{
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
            "[InputProbe] v0.5.0 loaded. Use !inputprobe on and !inputprobe capture key 5|6|7|8|9|0."
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
            else
            {
                CleanupTrackedEntitiesWithoutPlayer(state, logResult: false);
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
                context.Reply("[InputProbe] ON. Use !inputprobe capture key 5|6|7|8|9|0.");
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
                context.Reply(
                    "[InputProbe] Usage: !inputprobe on|off|status|capture key 5|6|7|8|9|0|capture all|capture off"
                );
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
            $"[InputProbe] ON. Capture: {(state.CaptureEnabled ? state.CaptureLabel : "off")}, " +
            $"trackedEntities: {state.TrackedEntities.Count}, hudOwned: {state.OwnsWeaponHudHideBit}."
        );
    }

    private void SetCapture(ICommandContext context, IPlayer player)
    {
        if (context.Args.Length < 2)
        {
            context.Reply(
                "[InputProbe] Usage: !inputprobe capture key 5|6|7|8|9|0 | capture all | capture off"
            );
            return;
        }

        if (!_states.TryGetValue(player.PlayerID, out var state))
        {
            state = new ProbeState();
            _states[player.PlayerID] = state;
        }

        var mode = context.Args[1].Trim().ToLowerInvariant();
        switch (mode)
        {
            case "off":
                StopCapture(player, state);
                context.Reply("[InputProbe] Capture OFF. Test entities removed and HUD restored.");
                return;

            case "all":
                StartCapture(context, player, state, CaptureTestItems, "all-5-0");
                return;

            case "key":
                if (context.Args.Length < 3)
                {
                    context.Reply("[InputProbe] Usage: !inputprobe capture key 5|6|7|8|9|0");
                    return;
                }

                var key = context.Args[2].Trim();
                var item = CaptureTestItems.FirstOrDefault(testItem => testItem.Key == key);
                if (item == default)
                {
                    context.Reply("[InputProbe] Supported keys: 5,6,7,8,9,0.");
                    return;
                }

                StartCapture(context, player, state, [item], $"key-{key}");
                return;

            default:
                context.Reply(
                    "[InputProbe] Usage: !inputprobe capture key 5|6|7|8|9|0 | capture all | capture off"
                );
                return;
        }
    }

    private void StartCapture(
        ICommandContext context,
        IPlayer player,
        ProbeState state,
        IReadOnlyList<CaptureTestItem> testItems,
        string captureLabel)
    {
        if (state.CaptureEnabled || state.TrackedEntities.Count > 0 || state.OwnsWeaponHudHideBit)
        {
            StopCapture(player, state);
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
        state.CaptureLabel = captureLabel;
        state.SeenCommands.Clear();
        state.TrackedEntities.Clear();

        HideWeaponHud(player, state, pawn);

        var activeBeforeInjection = weaponServices.ActiveWeapon.IsValid
            ? weaponServices.ActiveWeapon.Value
            : null;

        foreach (var testItem in testItems)
        {
            InjectTestItem(player, state, itemServices, weaponServices, testItem);
        }

        if (activeBeforeInjection is { IsValid: true }
            && weaponServices.MyValidWeapons.Any(weapon => weapon.Index == activeBeforeInjection.Index))
        {
            weaponServices.SelectWeapon(activeBeforeInjection);
        }

        Core.Logger.LogInformation(
            "[InputProbe][CAPTURE_CONTROL] player={PlayerId} enabled mode={Mode} tracked={TrackedCount} hideHud={HideHud}",
            player.PlayerID,
            state.CaptureLabel,
            state.TrackedEntities.Count,
            pawn.HideHUD
        );
        LogInventory(player, state);

        context.Reply(
            $"[InputProbe] Capture ON ({state.CaptureLabel}). HUD hidden. Press the requested key repeatedly; selection will be suppressed if CS2 attached the item."
        );
    }

    private void InjectTestItem(
        IPlayer player,
        ProbeState state,
        CCSPlayer_ItemServices itemServices,
        CCSPlayer_WeaponServices weaponServices,
        CaptureTestItem testItem)
    {
        var existing = weaponServices.MyValidWeapons.FirstOrDefault(weapon =>
            MatchesDesignerName(testItem, weapon.DesignerName)
        );

        if (existing is { IsValid: true })
        {
            Core.Logger.LogInformation(
                "[InputProbe][INJECT] player={PlayerId} key={Key} slot={Slot} weapon={Weapon} entity={EntityIndex} status=existing-attached",
                player.PlayerID,
                testItem.Key,
                testItem.SlotCommand,
                existing.DesignerName,
                existing.Index
            );
            return;
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
                return;
            }

            state.TrackedEntities[injected.Index] = injected.DesignerName;

            var attached = weaponServices.MyValidWeapons.Any(weapon =>
                weapon.Index == injected.Index
                && weapon.DesignerName.Equals(injected.DesignerName, StringComparison.OrdinalIgnoreCase)
            );

            Core.Logger.LogInformation(
                "[InputProbe][INJECT] player={PlayerId} key={Key} slot={Slot} weapon={Weapon} entity={EntityIndex} status={Status}",
                player.PlayerID,
                testItem.Key,
                testItem.SlotCommand,
                injected.DesignerName,
                injected.Index,
                attached ? "created-attached" : "created-not-attached"
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
        state.CaptureLabel = "-";
        state.SeenCommands.Clear();

        var weaponServices = player.PlayerPawn?.WeaponServices;

        foreach (var (entityIndex, expectedDesignerName) in state.TrackedEntities.ToArray())
        {
            var removedFromInventory = false;

            if (weaponServices is not null)
            {
                var inventoryWeapon = weaponServices.MyValidWeapons.FirstOrDefault(weapon =>
                    weapon.Index == entityIndex
                    && weapon.DesignerName.Equals(expectedDesignerName, StringComparison.OrdinalIgnoreCase)
                );

                if (inventoryWeapon is { IsValid: true })
                {
                    try
                    {
                        weaponServices.RemoveWeapon(inventoryWeapon);
                        removedFromInventory = true;
                        if (logResult)
                        {
                            Core.Logger.LogInformation(
                                "[InputProbe][CLEANUP] player={PlayerId} weapon={Weapon} entity={EntityIndex} status=removed-from-inventory",
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
                            "[InputProbe][CLEANUP] player={PlayerId} weapon={Weapon} entity={EntityIndex} status=remove-from-inventory-failed",
                            player.PlayerID,
                            expectedDesignerName,
                            entityIndex
                        );
                    }
                }
            }

            if (removedFromInventory)
            {
                continue;
            }

            DespawnTrackedEntity(entityIndex, expectedDesignerName, player.PlayerID, logResult);
        }

        state.TrackedEntities.Clear();
        RestoreWeaponHud(player, state);

        if (logResult)
        {
            Core.Logger.LogInformation("[InputProbe][CAPTURE_CONTROL] player={PlayerId} disabled", player.PlayerID);
            LogInventory(player, state);
        }
    }

    private void CleanupTrackedEntitiesWithoutPlayer(ProbeState state, bool logResult)
    {
        foreach (var (entityIndex, expectedDesignerName) in state.TrackedEntities.ToArray())
        {
            DespawnTrackedEntity(entityIndex, expectedDesignerName, playerId: -1, logResult);
        }

        state.TrackedEntities.Clear();
        state.SeenCommands.Clear();
        state.CaptureEnabled = false;
        state.CaptureLabel = "-";
        state.OwnsWeaponHudHideBit = false;
    }

    private void DespawnTrackedEntity(
        uint entityIndex,
        string expectedDesignerName,
        int playerId,
        bool logResult)
    {
        try
        {
            var entity = Core.EntitySystem.GetEntityByIndex(entityIndex);
            if (entity is not { IsValid: true }
                || !entity.DesignerName.Equals(expectedDesignerName, StringComparison.OrdinalIgnoreCase))
            {
                if (logResult)
                {
                    Core.Logger.LogInformation(
                        "[InputProbe][CLEANUP] player={PlayerId} weapon={Weapon} entity={EntityIndex} status=already-gone",
                        playerId,
                        expectedDesignerName,
                        entityIndex
                    );
                }
                return;
            }

            entity.Despawn();
            if (logResult)
            {
                Core.Logger.LogInformation(
                    "[InputProbe][CLEANUP] player={PlayerId} weapon={Weapon} entity={EntityIndex} status=despawned-orphan",
                    playerId,
                    expectedDesignerName,
                    entityIndex
                );
            }
        }
        catch (Exception exception)
        {
            Core.Logger.LogWarning(
                exception,
                "[InputProbe][CLEANUP] player={PlayerId} weapon={Weapon} entity={EntityIndex} status=despawn-failed",
                playerId,
                expectedDesignerName,
                entityIndex
            );
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
                state,
                baseCmd,
                usercmd.CommandNumber,
                baseCmd.Weaponselect,
                logSelection: isFirstCopy
            );
        }
    }

    private void SuppressCaptureSelection(
        IPlayer player,
        ProbeState state,
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
                    "[InputProbe][CAPTURE] player={PlayerId} mode={Mode} cmd={CommandNumber} key={Key} slot={Slot} weapon={Weapon} entity={EntityIndex} activeBefore={ActiveWeapon} suppressed=True",
                    player.PlayerID,
                    state.CaptureLabel,
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
            if (!MatchesDesignerName(testItem, designerName))
            {
                continue;
            }

            key = testItem.Key;
            slotCommand = testItem.SlotCommand;
            return true;
        }

        key = string.Empty;
        slotCommand = string.Empty;
        return false;
    }

    private static bool MatchesDesignerName(CaptureTestItem testItem, string designerName)
    {
        if (designerName.Equals(testItem.DesignerName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return testItem.Key == "0"
            && designerName.Equals("weapon_incgrenade", StringComparison.OrdinalIgnoreCase);
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

    private void LogInventory(IPlayer player, ProbeState state)
    {
        var pawn = player.PlayerPawn;
        var weaponServices = pawn?.WeaponServices;
        if (pawn is not { IsValid: true } || weaponServices is null)
        {
            Core.Logger.LogInformation(
                "[InputProbe][INVENTORY] player={PlayerId} mode={Mode} unavailable",
                player.PlayerID,
                state.CaptureLabel
            );
            return;
        }

        var inventory = weaponServices.MyValidWeapons
            .OrderBy(weapon => weapon.Index)
            .Select(weapon => $"{weapon.Index}:{weapon.DesignerName}")
            .ToArray();

        Core.Logger.LogInformation(
            "[InputProbe][INVENTORY] player={PlayerId} mode={Mode} hideHud={HideHud} active={ActiveWeapon} weapons=[{Weapons}] tracked=[{Tracked}]",
            player.PlayerID,
            state.CaptureLabel,
            pawn.HideHUD,
            DescribeActiveWeapon(player),
            inventory.Length == 0 ? "-" : string.Join(',', inventory),
            state.TrackedEntities.Count == 0
                ? "-"
                : string.Join(',', state.TrackedEntities.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}"))
        );
    }

    private void OnClientDisconnected(IOnClientDisconnectedEvent @event)
    {
        if (!_states.TryGetValue(@event.PlayerId, out var state))
        {
            return;
        }

        CleanupTrackedEntitiesWithoutPlayer(state, logResult: false);
        _states.Remove(@event.PlayerId);
    }

    private sealed class ProbeState
    {
        public bool CaptureEnabled { get; set; }
        public string CaptureLabel { get; set; } = "-";
        public bool OwnsWeaponHudHideBit { get; set; }
        public Dictionary<uint, string> TrackedEntities { get; } = [];
        public HashSet<uint> SeenCommands { get; } = [];

        public bool MarkCommandSeen(uint commandNumber) => SeenCommands.Add(commandNumber);
    }

    private readonly record struct CaptureTestItem(
        string Key,
        string SlotCommand,
        string DesignerName
    );
}
