using System.Diagnostics;
using System.Numerics;
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
    Version = "0.3.0",
    Name = "Elysium Input Probe",
    Author = "Elysium",
    Description = "Diagnostic probe for CS2 usercmd, key-state and client-command input."
)]
public sealed class ElysiumInputProbePlugin(ISwiftlyCore core) : BasePlugin(core)
{
    private static readonly CaptureTestItem[] CaptureTestItems =
    [
        new("6", "slot6", "weapon_hegrenade"),
        new("7", "slot7", "weapon_flashbang"),
        new("8", "slot8", "weapon_smokegrenade"),
        new("9", "slot9", "weapon_decoy"),
        new("0", "slot10", "weapon_molotov")
    ];

    private readonly Dictionary<int, ProbeState> _states = [];

    private Guid _commandId = Guid.Empty;
    private Guid _clientCommandHookId = Guid.Empty;

    public override void Load(bool hotReload)
    {
        _commandId = Core.Command.RegisterCommand(
            "inputprobe",
            HandleProbeCommand,
            helpText: "Toggles ElysiumInputProbe for the calling player."
        );

        _clientCommandHookId = Core.Command.HookClientCommand(OnClientCommand);
        Core.Event.OnClientKeyStateChanged += OnClientKeyStateChanged;
        Core.Event.OnClientDisconnected += OnClientDisconnected;
        Core.GameHooks.Controller.ProcessUsercmds.Pre += OnProcessUsercmds;

        Core.Logger.LogInformation(
            "[InputProbe] Loaded. Use !inputprobe on, !inputprobe capture on, !inputprobe mode changes|all and !inputprobe mark <label>."
        );
    }

    public override void Unload()
    {
        // Диагностические предметы не должны переживать hot-reload probe.
        foreach (var (playerId, state) in _states.ToArray())
        {
            var player = Core.PlayerManager.GetPlayer(playerId);
            if (player is { IsValid: true })
            {
                StopCapture(player, state, logResult: false);
            }
        }

        Core.GameHooks.Controller.ProcessUsercmds.Pre -= OnProcessUsercmds;
        Core.Event.OnClientKeyStateChanged -= OnClientKeyStateChanged;
        Core.Event.OnClientDisconnected -= OnClientDisconnected;

        if (_clientCommandHookId != Guid.Empty)
        {
            Core.Command.UnhookClientCommand(_clientCommandHookId);
            _clientCommandHookId = Guid.Empty;
        }

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

        var playerId = player.PlayerID;
        var action = context.Args.Length == 0
            ? "toggle"
            : context.Args[0].Trim().ToLowerInvariant();

        switch (action)
        {
            case "on":
                Enable(playerId);
                context.Reply("[InputProbe] ON. Mode: changes.");
                break;

            case "off":
                Disable(player);
                context.Reply("[InputProbe] OFF.");
                break;

            case "toggle":
                if (_states.ContainsKey(playerId))
                {
                    Disable(player);
                    context.Reply("[InputProbe] OFF.");
                }
                else
                {
                    Enable(playerId);
                    context.Reply("[InputProbe] ON. Mode: changes.");
                }
                break;

            case "status":
                if (!_states.TryGetValue(playerId, out var currentState))
                {
                    context.Reply("[InputProbe] OFF.");
                    break;
                }

                context.Reply(
                    $"[InputProbe] ON. Mode: {currentState.Mode.ToString().ToLowerInvariant()}, marker: {currentState.Marker}, " +
                    $"capture: {(currentState.CaptureEnabled ? "on" : "off")}, injected: {currentState.InjectedWeapons.Count}."
                );
                break;

            case "mode":
                SetMode(context, playerId);
                break;

            case "mark":
                SetMarker(context, player);
                break;

            case "capture":
                SetCapture(context, player);
                break;

            case "reset":
                Reset(playerId);
                context.Reply("[InputProbe] State baseline reset.");
                break;

            default:
                context.Reply(
                    "[InputProbe] Usage: !inputprobe [on|off|status|reset|capture on|capture off|mode changes|mode all|mark <label>]"
                );
                break;
        }
    }

    private void Enable(int playerId)
    {
        if (_states.TryGetValue(playerId, out var existing))
        {
            existing.ResetBaseline();
            return;
        }

        _states[playerId] = new ProbeState();
        Core.Logger.LogInformation("[InputProbe][CONTROL] player={PlayerId} enabled", playerId);
    }

    private void Disable(IPlayer player)
    {
        var playerId = player.PlayerID;
        if (!_states.TryGetValue(playerId, out var state))
        {
            return;
        }

        StopCapture(player, state);
        _states.Remove(playerId);

        Core.Logger.LogInformation("[InputProbe][CONTROL] player={PlayerId} disabled", playerId);
    }

    private void Reset(int playerId)
    {
        if (!_states.TryGetValue(playerId, out var state))
        {
            state = new ProbeState();
            _states[playerId] = state;
        }

        state.ResetBaseline();
    }

    private void SetMode(ICommandContext context, int playerId)
    {
        if (context.Args.Length < 2)
        {
            context.Reply("[InputProbe] Usage: !inputprobe mode changes|all");
            return;
        }

        if (!_states.TryGetValue(playerId, out var state))
        {
            state = new ProbeState();
            _states[playerId] = state;
        }

        switch (context.Args[1].Trim().ToLowerInvariant())
        {
            case "changes":
                state.Mode = ProbeMode.Changes;
                state.ResetBaseline();
                context.Reply("[InputProbe] Mode: changes.");
                break;

            case "all":
                state.Mode = ProbeMode.All;
                state.ResetBaseline();
                context.Reply("[InputProbe] Mode: all. WARNING: this logs every usercmd for you.");
                break;

            default:
                context.Reply("[InputProbe] Usage: !inputprobe mode changes|all");
                break;
        }
    }

    private void SetMarker(ICommandContext context, IPlayer player)
    {
        if (context.Args.Length < 2)
        {
            context.Reply("[InputProbe] Usage: !inputprobe mark <label>");
            return;
        }

        var playerId = player.PlayerID;
        if (!_states.TryGetValue(playerId, out var state))
        {
            state = new ProbeState();
            _states[playerId] = state;
        }

        state.Marker = string.Join(" ", context.Args.Skip(1));
        state.ResetCommandDeduplication();

        Core.Logger.LogInformation(
            "[InputProbe][MARK] player={PlayerId} t={ElapsedMs:F3}ms marker={Marker}",
            playerId,
            state.ElapsedMilliseconds,
            state.Marker
        );

        LogInventory(player, state);
        context.Reply($"[InputProbe] Marker: {state.Marker}");
    }

    private void SetCapture(ICommandContext context, IPlayer player)
    {
        if (context.Args.Length < 2)
        {
            context.Reply("[InputProbe] Usage: !inputprobe capture on|off");
            return;
        }

        var playerId = player.PlayerID;
        if (!_states.TryGetValue(playerId, out var state))
        {
            state = new ProbeState();
            _states[playerId] = state;
        }

        switch (context.Args[1].Trim().ToLowerInvariant())
        {
            case "on":
                StartCapture(context, player, state);
                break;

            case "off":
                StopCapture(player, state);
                context.Reply("[InputProbe] Capture OFF. Injected test items removed.");
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
            LogInventory(player, state);
            return;
        }

        var pawn = player.PlayerPawn;
        var itemServices = pawn?.ItemServices;
        var weaponServices = pawn?.WeaponServices;
        if (pawn is not { IsValid: true } || itemServices is null || weaponServices is null)
        {
            context.Reply("[InputProbe] Capture requires a valid alive player pawn with item/weapon services.");
            return;
        }

        state.CaptureEnabled = true;
        state.Marker = "capture-6-0";
        state.ResetBaseline();
        state.InjectedWeapons.Clear();

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

        // GiveItem может изменить активное оружие. Для чистого теста возвращаем то,
        // которое было выбрано до временного наполнения слотов.
        if (activeBeforeInjection is { IsValid: true }
            && weaponServices.MyValidWeapons.Any(weapon => weapon.Index == activeBeforeInjection.Index))
        {
            weaponServices.SelectWeapon(activeBeforeInjection);
        }

        Core.Logger.LogInformation(
            "[InputProbe][CAPTURE_CONTROL] player={PlayerId} enabled injected={InjectedCount}",
            player.PlayerID,
            state.InjectedWeapons.Count
        );
        LogInventory(player, state);

        context.Reply(
            $"[InputProbe] Capture ON. Injected {state.InjectedWeapons.Count} missing test items. Press 6,7,8,9,0; selections will be suppressed."
        );
    }

    private void StopCapture(IPlayer player, ProbeState state, bool logResult = true)
    {
        state.CaptureEnabled = false;

        var weaponServices = player.PlayerPawn?.WeaponServices;
        if (weaponServices is not null && state.InjectedWeapons.Count > 0)
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
        state.ResetCommandDeduplication();

        if (logResult)
        {
            Core.Logger.LogInformation(
                "[InputProbe][CAPTURE_CONTROL] player={PlayerId} disabled",
                player.PlayerID
            );
            LogInventory(player, state);
        }
    }

    private void LogInventory(IPlayer player, ProbeState state)
    {
        var pawn = player.PlayerPawn;
        var weaponServices = pawn?.WeaponServices;
        if (pawn is not { IsValid: true } || weaponServices is null)
        {
            Core.Logger.LogInformation(
                "[InputProbe][INVENTORY] player={PlayerId} t={ElapsedMs:F3}ms marker={Marker} unavailable",
                player.PlayerID,
                state.ElapsedMilliseconds,
                state.Marker
            );
            return;
        }

        var activeWeapon = weaponServices.ActiveWeapon.IsValid
            ? weaponServices.ActiveWeapon.Value
            : null;
        var activeDescription = activeWeapon is { IsValid: true }
            ? $"{activeWeapon.Index}:{activeWeapon.DesignerName}"
            : "-";
        var inventory = weaponServices.MyValidWeapons
            .OrderBy(weapon => weapon.Index)
            .Select(weapon => $"{weapon.Index}:{weapon.DesignerName}")
            .ToArray();

        Core.Logger.LogInformation(
            "[InputProbe][INVENTORY] player={PlayerId} t={ElapsedMs:F3}ms marker={Marker} active={ActiveWeapon} weapons=[{Weapons}]",
            player.PlayerID,
            state.ElapsedMilliseconds,
            state.Marker,
            activeDescription,
            inventory.Length == 0 ? "-" : string.Join(',', inventory)
        );
    }

    private HookResult OnClientCommand(int playerId, string commandLine)
    {
        if (!_states.TryGetValue(playerId, out var state))
        {
            return HookResult.Continue;
        }

        Core.Logger.LogInformation(
            "[InputProbe][CLIENT_COMMAND] player={PlayerId} t={ElapsedMs:F3}ms marker={Marker} command={CommandLine}",
            playerId,
            state.ElapsedMilliseconds,
            state.Marker,
            commandLine
        );

        return HookResult.Continue;
    }

    private void OnClientKeyStateChanged(IOnClientKeyStateChangedEvent @event)
    {
        if (!_states.TryGetValue(@event.PlayerId, out var state))
        {
            return;
        }

        Core.Logger.LogInformation(
            "[InputProbe][KEY_STATE] player={PlayerId} t={ElapsedMs:F3}ms marker={Marker} key={Key} pressed={Pressed}",
            @event.PlayerId,
            state.ElapsedMilliseconds,
            state.Marker,
            @event.Key,
            @event.Pressed
        );
    }

    private void OnProcessUsercmds(ref ProcessUsercmdsPreContext context)
    {
        var player = context.Params.Player;
        var playerId = player.PlayerID;
        if (!_states.TryGetValue(playerId, out var state))
        {
            return;
        }

        foreach (var usercmd in context.Params.Usercmds)
        {
            // ProcessUsercmds может повторно принести уже обработанный command number.
            // Дедуплицируем логи/callback, но подавление input применяем к каждой копии.
            var isNewCommand = state.TryAcceptCommand(usercmd.CommandNumber);
            var baseCmd = usercmd.CSGOUserCmd.Base;
            var protobufButtons = baseCmd.ButtonsPb;
            var schemaButtons = usercmd.ButtonState.ButtonStates;

            var snapshot = new UserCmdSnapshot(
                CommandNumber: usercmd.CommandNumber,
                LegacyCommandNumber: baseCmd.LegacyCommandNumber,
                ClientTick: baseCmd.ClientTick,
                ProtobufState1: protobufButtons.Buttonstate1,
                ProtobufState2: protobufButtons.Buttonstate2,
                ProtobufState3: protobufButtons.Buttonstate3,
                SchemaState1: schemaButtons[0],
                SchemaState2: schemaButtons[1],
                SchemaState3: schemaButtons[2],
                WeaponSelect: baseCmd.Weaponselect,
                Impulse: baseCmd.Impulse,
                CmdFlags: baseCmd.CmdFlags
            );

            if (isNewCommand)
            {
                var buttonSubticks = CaptureButtonSubticks(baseCmd.SubtickMoves);
                var shouldLog = state.Mode == ProbeMode.All
                    || state.LastSnapshot is null
                    || HasMeaningfulChange(state.LastSnapshot.Value, snapshot)
                    || buttonSubticks.Count > 0;

                if (shouldLog)
                {
                    LogUserCmd(player, state, snapshot, buttonSubticks);
                }

                state.LastSnapshot = snapshot;
            }

            if (state.CaptureEnabled && snapshot.WeaponSelect != 0)
            {
                SuppressCaptureSelection(
                    player,
                    state,
                    baseCmd,
                    snapshot.CommandNumber,
                    snapshot.WeaponSelect,
                    logSelection: isNewCommand
                );
            }
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
                    "[InputProbe][CAPTURE] player={PlayerId} t={ElapsedMs:F3}ms marker={Marker} cmd={CommandNumber} key={Key} slot={Slot} " +
                    "weapon={Weapon} entity={EntityIndex} activeBefore={ActiveWeapon} suppressed=True",
                    player.PlayerID,
                    state.ElapsedMilliseconds,
                    state.Marker,
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

        // На CT slot10 может разрешиться в incendiary вместо molotov.
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

    private void LogUserCmd(
        IPlayer player,
        ProbeState state,
        UserCmdSnapshot snapshot,
        IReadOnlyList<ButtonSubtickSnapshot> buttonSubticks)
    {
        var currentPb = snapshot.ProtobufStates;
        var currentSchema = snapshot.SchemaStates;
        var activeBits = FormatActiveBits(currentPb);
        var changedBits = state.LastSnapshot is { } previous
            ? FormatChangedBits(previous.ProtobufStates, currentPb)
            : "baseline";
        var subticks = FormatSubticks(buttonSubticks);
        var statesMatch = currentPb.SequenceEqual(currentSchema);
        var weaponSelectDescription = DescribeWeaponSelect(player, snapshot.WeaponSelect);
        var activeWeaponDescription = DescribeActiveWeapon(player);

        Core.Logger.LogInformation(
            "[InputProbe][USERCMD] player={PlayerId} t={ElapsedMs:F3}ms marker={Marker} cmd={CommandNumber} legacy={LegacyCommandNumber} tick={ClientTick} " +
            "pb=[{Pb1},{Pb2},{Pb3}] schema=[{Schema1},{Schema2},{Schema3}] statesMatch={StatesMatch} activeBits={ActiveBits} changedBits={ChangedBits} " +
            "weaponSelect={WeaponSelect} weapon={WeaponDescription} activeBefore={ActiveWeapon} impulse={Impulse} flags={CmdFlags} subticks={Subticks}",
            player.PlayerID,
            state.ElapsedMilliseconds,
            state.Marker,
            snapshot.CommandNumber,
            snapshot.LegacyCommandNumber,
            snapshot.ClientTick,
            Hex(snapshot.ProtobufState1),
            Hex(snapshot.ProtobufState2),
            Hex(snapshot.ProtobufState3),
            Hex(snapshot.SchemaState1),
            Hex(snapshot.SchemaState2),
            Hex(snapshot.SchemaState3),
            statesMatch,
            activeBits,
            changedBits,
            snapshot.WeaponSelect,
            weaponSelectDescription,
            activeWeaponDescription,
            snapshot.Impulse,
            snapshot.CmdFlags,
            subticks
        );
    }

    private string DescribeWeaponSelect(IPlayer player, int weaponSelect)
    {
        if (weaponSelect == 0)
        {
            return "-";
        }

        try
        {
            // CBaseUserCmdPB::weaponselect передаёт packed entity handle.
            // Нижние 14 бит соответствуют индексу entity для этого wire-формата.
            var candidateIndex = (uint)(weaponSelect & 0x3FFF);
            var entity = Core.EntitySystem.GetEntityByIndex(candidateIndex);
            var designerName = entity is { IsValid: true }
                ? entity.DesignerName
                : "?";
            var inInventory = player.PlayerPawn?.WeaponServices?.MyValidWeapons
                .Any(weapon => weapon.Index == candidateIndex) == true;

            return $"packed={weaponSelect};index={candidateIndex};name={designerName};inInventory={inInventory}";
        }
        catch (Exception exception)
        {
            Core.Logger.LogDebug(
                exception,
                "[InputProbe] Failed to resolve weaponSelect={WeaponSelect} for player={PlayerId}",
                weaponSelect,
                player.PlayerID
            );
            return $"packed={weaponSelect};unresolved";
        }
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

    private static bool HasMeaningfulChange(UserCmdSnapshot previous, UserCmdSnapshot current)
    {
        return !previous.ProtobufStates.SequenceEqual(current.ProtobufStates)
            || !previous.SchemaStates.SequenceEqual(current.SchemaStates)
            || previous.WeaponSelect != current.WeaponSelect
            || previous.Impulse != current.Impulse;
    }

    private static List<ButtonSubtickSnapshot> CaptureButtonSubticks(
        IEnumerable<CSubtickMoveStep> subtickMoves)
    {
        List<ButtonSubtickSnapshot> result = [];

        foreach (var step in subtickMoves)
        {
            if (step.Button == 0)
            {
                continue;
            }

            result.Add(new ButtonSubtickSnapshot(step.Button, step.Pressed, step.When));
        }

        return result;
    }

    private static string FormatSubticks(IReadOnlyList<ButtonSubtickSnapshot> subticks)
    {
        if (subticks.Count == 0)
        {
            return "-";
        }

        return string.Join(
            ",",
            subticks.Select(step =>
                $"{Hex(step.Button)}:{(step.Pressed ? '+' : '-')}@{step.When:F3}:{DescribeSingleButton(step.Button)}"
            )
        );
    }

    private static string FormatActiveBits(IReadOnlyList<ulong> states)
    {
        List<string> bits = [];

        for (var wordIndex = 0; wordIndex < states.Count; wordIndex++)
        {
            var buttonState = states[wordIndex];
            for (var bit = 0; bit < 64; bit++)
            {
                var mask = 1UL << bit;
                if ((buttonState & mask) == 0)
                {
                    continue;
                }

                var globalBit = wordIndex * 64 + bit;
                bits.Add($"{globalBit}:{KnownButtonName(globalBit)}");
            }
        }

        return bits.Count == 0 ? "-" : string.Join(",", bits);
    }

    private static string FormatChangedBits(IReadOnlyList<ulong> previous, IReadOnlyList<ulong> current)
    {
        List<string> bits = [];
        var wordCount = Math.Min(previous.Count, current.Count);

        for (var wordIndex = 0; wordIndex < wordCount; wordIndex++)
        {
            var changed = previous[wordIndex] ^ current[wordIndex];
            for (var bit = 0; bit < 64; bit++)
            {
                var mask = 1UL << bit;
                if ((changed & mask) == 0)
                {
                    continue;
                }

                var globalBit = wordIndex * 64 + bit;
                var pressed = (current[wordIndex] & mask) != 0;
                bits.Add($"{globalBit}:{KnownButtonName(globalBit)}{(pressed ? '+' : '-')}");
            }
        }

        return bits.Count == 0 ? "-" : string.Join(",", bits);
    }

    private static string DescribeSingleButton(ulong button)
    {
        if (!BitOperations.IsPow2(button))
        {
            return "multi/unknown";
        }

        var bit = BitOperations.TrailingZeroCount(button);
        return $"bit={bit}:{KnownButtonName(bit)}";
    }

    private static string KnownButtonName(int bit)
    {
        return bit switch
        {
            0 => "Mouse1",
            1 => "Space",
            2 => "Ctrl",
            3 => "W",
            4 => "S",
            5 => "E",
            6 => "Esc",
            7 => "A",
            8 => "D",
            9 => "A2",
            10 => "D2",
            11 => "Mouse2",
            12 => "Run?",
            13 => "R",
            14 => "Alt",
            15 => "Alt2",
            16 => "Shift",
            17 => "Speed?",
            18 => "Shift2",
            19 => "HudZoom?",
            20 => "Weapon1?",
            21 => "Weapon2?",
            22 => "BullRush?",
            23 => "Grenade1?",
            24 => "Grenade2?",
            25 => "LookSpin?",
            33 => "Tab",
            35 => "F",
            _ => "Unknown"
        };
    }

    private static string Hex(ulong value) => $"0x{value:X16}";

    private void OnClientDisconnected(IOnClientDisconnectedEvent @event)
    {
        _states.Remove(@event.PlayerId);
    }

    private enum ProbeMode
    {
        Changes,
        All
    }

    private sealed class ProbeState
    {
        private long _startedAt = Stopwatch.GetTimestamp();

        public ProbeMode Mode { get; set; } = ProbeMode.Changes;
        public string Marker { get; set; } = "-";
        public UserCmdSnapshot? LastSnapshot { get; set; }
        public uint? HighestCommandNumber { get; private set; }
        public bool CaptureEnabled { get; set; }
        public Dictionary<uint, string> InjectedWeapons { get; } = [];

        public double ElapsedMilliseconds => Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds;

        public bool TryAcceptCommand(uint commandNumber)
        {
            if (HighestCommandNumber is { } highest && commandNumber <= highest)
            {
                return false;
            }

            HighestCommandNumber = commandNumber;
            return true;
        }

        public void ResetCommandDeduplication()
        {
            HighestCommandNumber = null;
        }

        public void ResetBaseline()
        {
            LastSnapshot = null;
            HighestCommandNumber = null;
            _startedAt = Stopwatch.GetTimestamp();
        }
    }

    private readonly record struct CaptureTestItem(
        string Key,
        string SlotCommand,
        string DesignerName
    );

    private readonly record struct UserCmdSnapshot(
        uint CommandNumber,
        int LegacyCommandNumber,
        int ClientTick,
        ulong ProtobufState1,
        ulong ProtobufState2,
        ulong ProtobufState3,
        ulong SchemaState1,
        ulong SchemaState2,
        ulong SchemaState3,
        int WeaponSelect,
        int Impulse,
        int CmdFlags
    )
    {
        public ulong[] ProtobufStates => [ProtobufState1, ProtobufState2, ProtobufState3];
        public ulong[] SchemaStates => [SchemaState1, SchemaState2, SchemaState3];
    }

    private readonly record struct ButtonSubtickSnapshot(
        ulong Button,
        bool Pressed,
        float When
    );
}
