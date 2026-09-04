using System.Diagnostics;
using System.Numerics;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Plugins;

namespace ElysiumInputProbe.Core;

[PluginMetadata(
    Id = "ElysiumInputProbe.Core",
    Version = "0.1.0",
    Name = "Elysium Input Probe",
    Author = "Elysium",
    Description = "Diagnostic probe for CS2 usercmd, key-state and client-command input."
)]
public sealed class ElysiumInputProbePlugin(ISwiftlyCore core) : BasePlugin(core)
{
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
            "[InputProbe] Loaded. Use !inputprobe on, !inputprobe mode changes|all and !inputprobe mark <label>."
        );
    }

    public override void Unload()
    {
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
                Disable(playerId);
                context.Reply("[InputProbe] OFF.");
                break;

            case "toggle":
                if (_states.ContainsKey(playerId))
                {
                    Disable(playerId);
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
                    $"[InputProbe] ON. Mode: {currentState.Mode.ToString().ToLowerInvariant()}, marker: {currentState.Marker}."
                );
                break;

            case "mode":
                SetMode(context, playerId);
                break;

            case "mark":
                SetMarker(context, playerId);
                break;

            case "reset":
                Reset(playerId);
                context.Reply("[InputProbe] State baseline reset.");
                break;

            default:
                context.Reply(
                    "[InputProbe] Usage: !inputprobe [on|off|status|reset|mode changes|mode all|mark <label>]"
                );
                break;
        }
    }

    private void Enable(int playerId)
    {
        _states[playerId] = new ProbeState();
        Core.Logger.LogInformation("[InputProbe][CONTROL] player={PlayerId} enabled", playerId);
    }

    private void Disable(int playerId)
    {
        if (!_states.Remove(playerId))
        {
            return;
        }

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

    private void SetMarker(ICommandContext context, int playerId)
    {
        if (context.Args.Length < 2)
        {
            context.Reply("[InputProbe] Usage: !inputprobe mark <label>");
            return;
        }

        if (!_states.TryGetValue(playerId, out var state))
        {
            state = new ProbeState();
            _states[playerId] = state;
        }

        state.Marker = string.Join(' ', context.Args.Skip(1));

        Core.Logger.LogInformation(
            "[InputProbe][MARK] player={PlayerId} t={ElapsedMs:F3}ms marker={Marker}",
            playerId,
            state.ElapsedMilliseconds,
            state.Marker
        );

        context.Reply($"[InputProbe] Marker: {state.Marker}");
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
        var playerId = context.Params.Player.PlayerID;
        if (!_states.TryGetValue(playerId, out var state))
        {
            return;
        }

        foreach (var usercmd in context.Params.Usercmds)
        {
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

            var buttonSubticks = CaptureButtonSubticks(baseCmd.SubtickMoves);
            var shouldLog = state.Mode == ProbeMode.All
                || state.LastSnapshot is null
                || HasMeaningfulChange(state.LastSnapshot.Value, snapshot)
                || buttonSubticks.Count > 0;

            if (shouldLog)
            {
                LogUserCmd(playerId, state, snapshot, buttonSubticks);
            }

            state.LastSnapshot = snapshot;
        }
    }

    private void LogUserCmd(
        int playerId,
        ProbeState state,
        UserCmdSnapshot snapshot,
        IReadOnlyList<ButtonSubtickSnapshot> buttonSubticks)
    {
        var activeBits = FormatActiveBits(snapshot.ProtobufState1);
        var changedBits = state.LastSnapshot is { } previous
            ? FormatChangedBits(previous.ProtobufState1, snapshot.ProtobufState1)
            : "baseline";
        var subticks = FormatSubticks(buttonSubticks);

        Core.Logger.LogInformation(
            "[InputProbe][USERCMD] player={PlayerId} t={ElapsedMs:F3}ms marker={Marker} cmd={CommandNumber} legacy={LegacyCommandNumber} tick={ClientTick} " +
            "pb=[{Pb1},{Pb2},{Pb3}] schema=[{Schema1},{Schema2},{Schema3}] activeBits={ActiveBits} changedBits={ChangedBits} " +
            "weaponSelect={WeaponSelect} impulse={Impulse} flags={CmdFlags} subticks={Subticks}",
            playerId,
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
            activeBits,
            changedBits,
            snapshot.WeaponSelect,
            snapshot.Impulse,
            snapshot.CmdFlags,
            subticks
        );
    }

    private static bool HasMeaningfulChange(UserCmdSnapshot previous, UserCmdSnapshot current)
    {
        return previous.ProtobufState1 != current.ProtobufState1
            || previous.ProtobufState2 != current.ProtobufState2
            || previous.ProtobufState3 != current.ProtobufState3
            || previous.SchemaState1 != current.SchemaState1
            || previous.SchemaState2 != current.SchemaState2
            || previous.SchemaState3 != current.SchemaState3
            || previous.WeaponSelect != current.WeaponSelect
            || previous.Impulse != current.Impulse;
    }

    private static List<ButtonSubtickSnapshot> CaptureButtonSubticks(
        IEnumerable<SwiftlyS2.Shared.ProtobufDefinitions.CSubtickMoveStep> subtickMoves)
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
            ',',
            subticks.Select(step =>
                $"{Hex(step.Button)}:{(step.Pressed ? '+' : '-')}@{step.When:F3}:{DescribeSingleButton(step.Button)}"
            )
        );
    }

    private static string FormatActiveBits(ulong state)
    {
        if (state == 0)
        {
            return "-";
        }

        List<string> bits = [];
        for (var bit = 0; bit < 64; bit++)
        {
            var mask = 1UL << bit;
            if ((state & mask) == 0)
            {
                continue;
            }

            bits.Add($"{bit}:{KnownButtonName(bit)}");
        }

        return string.Join(',', bits);
    }

    private static string FormatChangedBits(ulong previous, ulong current)
    {
        var changed = previous ^ current;
        if (changed == 0)
        {
            return "-";
        }

        List<string> bits = [];
        for (var bit = 0; bit < 64; bit++)
        {
            var mask = 1UL << bit;
            if ((changed & mask) == 0)
            {
                continue;
            }

            var pressed = (current & mask) != 0;
            bits.Add($"{bit}:{KnownButtonName(bit)}{(pressed ? '+' : '-')}");
        }

        return string.Join(',', bits);
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

        public double ElapsedMilliseconds => Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds;

        public void ResetBaseline()
        {
            LastSnapshot = null;
            _startedAt = Stopwatch.GetTimestamp();
        }
    }

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
    );

    private readonly record struct ButtonSubtickSnapshot(
        ulong Button,
        bool Pressed,
        float When
    );
}
