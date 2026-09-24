using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Convars;
using SwiftlyS2.Shared.Services;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class MapEngineAdapterTests
{
    [Fact]
    public void EmptyPoolKeepsUnlimitedLimitsWithoutSuppressingNativeMatchExit()
    {
        var f = new Fixture();

        f.ApplyPolicy(rotationEnabled: false);

        Assert.Equal("0.000000", f.Time.Value);
        Assert.Equal("0", f.Rounds.Value);
        Assert.Equal("0", f.Wins.Value);
        Assert.Equal("true", f.ChangeLevel.Value);
        Assert.Equal("true", f.Restart.Value);
        Assert.Equal(0, f.ChangeLevel.Writes);
        Assert.Equal(0, f.Restart.Writes);
    }

    [Fact]
    public void ClearingAnActivePoolRestoresMatchControlsAndKeepsUnlimitedLimits()
    {
        var f = new Fixture();
        f.ApplyPolicy(rotationEnabled: true);
        Assert.Equal("false", f.ChangeLevel.Value);
        Assert.Equal("false", f.Restart.Value);

        f.ApplyPolicy(rotationEnabled: false);
        f.ApplyPolicy(rotationEnabled: false);

        Assert.Equal("true", f.ChangeLevel.Value);
        Assert.Equal("true", f.Restart.Value);
        Assert.Equal(2, f.Restart.Writes);
        Assert.Equal("0.000000", f.Time.Value);
        Assert.Equal(3, f.Adapter.Overrides.Count);
    }

    [Fact]
    public void CanonicalBooleanAndFloatValuesAreNotWrittenAgainOnEveryRound()
    {
        var f = new Fixture();
        f.ApplyPolicy(rotationEnabled: true);
        for (var i = 0; i < 20; i++) f.ApplyPolicy(rotationEnabled: true);

        Assert.All(f.Values.Values, cvar => Assert.Equal(1, cvar.Writes));
        Assert.Equal("false", f.Adapter.Overrides["mp_match_end_restart"].Applied);
        Assert.Equal("0.000000", f.Adapter.Overrides["mp_timelimit"].Applied);
    }

    [Fact]
    public void UnloadRestoresCanonicalValuesAndIsIdempotent()
    {
        var f = new Fixture();
        f.ApplyPolicy(rotationEnabled: true);

        f.Unload();
        f.Unload();

        Assert.Equal("45.000000", f.Time.Value);
        Assert.Equal("24", f.Rounds.Value);
        Assert.Equal("13", f.Wins.Value);
        Assert.Equal("true", f.ChangeLevel.Value);
        Assert.Equal("true", f.Restart.Value);
        Assert.All(f.Values.Values, cvar => Assert.Equal(2, cvar.Writes));
        Assert.Empty(f.Adapter.Overrides);
    }

    [Fact]
    public void ReleaseDoesNotOverwriteSettingsChangedExternally()
    {
        var f = new Fixture();
        f.ApplyPolicy(rotationEnabled: true);
        f.Time.Value = "30.000000";
        f.Restart.Value = "true";

        f.ApplyPolicy(rotationEnabled: false);
        Assert.Equal(1, f.Restart.Writes);
        f.Time.Value = "20.000000";
        f.Unload();

        Assert.Equal("20.000000", f.Time.Value);
        Assert.Equal("true", f.Restart.Value);
        Assert.Equal(1, f.Restart.Writes);
    }

    [Fact]
    public void NewMapConfigurationBecomesTheNewRestorePoint()
    {
        var f = new Fixture();
        f.ApplyPolicy(rotationEnabled: true);
        f.Time.Value = "20.000000";
        f.Rounds.Value = "12";
        f.ApplyPolicy(rotationEnabled: true);
        f.Unload();

        Assert.Equal("20.000000", f.Time.Value);
        Assert.Equal("12", f.Rounds.Value);
    }

    [Fact]
    public void RefillingThePoolPreservesTheSettingsChosenWhileRotationWasInactive()
    {
        var f = new Fixture();
        f.ApplyPolicy(rotationEnabled: true);
        f.ApplyPolicy(rotationEnabled: false);
        f.Restart.Value = "false";
        f.ApplyPolicy(rotationEnabled: true);
        f.ApplyPolicy(rotationEnabled: false);

        Assert.Equal("false", f.Restart.Value);
        Assert.Equal("true", f.ChangeLevel.Value);
        Assert.Equal(2, f.Restart.Writes);
    }

    [Fact]
    public void AlreadyUnlimitedOrMissingConVarsDoNotCreateOverrides()
    {
        var f = new Fixture();
        f.Time.Value = "0.000000";
        f.Rounds.Value = "0";
        f.Wins.Value = "0";
        f.ChangeLevel.Value = "false";
        f.Values.Remove("mp_match_end_restart");

        f.ApplyPolicy(rotationEnabled: true);
        f.Unload();

        Assert.All(f.Values.Values, cvar => Assert.Equal(0, cvar.Writes));
        Assert.Empty(f.Adapter.Overrides);
    }

    [Fact]
    public void EmptyPoolWithOnlyRoundLimitChangesAndRestoresOnlyThatLimit()
    {
        var f = new Fixture();
        f.Time.Value = "0.000000";
        f.Rounds.Value = "30";
        f.Wins.Value = "0";
        f.ChangeLevel.Value = "false";
        f.Restart.Value = "false";

        f.ApplyPolicy(rotationEnabled: false);

        Assert.Equal("0", f.Rounds.Value);
        var owned = Assert.Single(f.Adapter.Overrides);
        Assert.Equal("mp_maxrounds", owned.Key);
        Assert.Equal("30", owned.Value.Original);
        Assert.Equal("0", owned.Value.Applied);

        f.Unload();

        Assert.Equal("30", f.Rounds.Value);
        Assert.Equal(2, f.Rounds.Writes);
        Assert.All(f.Values.Where(pair => pair.Key != "mp_maxrounds"), pair => Assert.Equal(0, pair.Value.Writes));
        Assert.Empty(f.Adapter.Overrides);
    }

    [Fact]
    public void AlreadyZeroServerFixtureDoesNotQueueAnyCommandOnLoadOrUnload()
    {
        var f = new Fixture();
        f.Time.Value = "0.000000";
        f.Rounds.Value = "0";
        f.Wins.Value = "0";
        f.ChangeLevel.Value = "false";
        f.Restart.Value = "false";

        f.ApplyPolicy(rotationEnabled: false);
        f.Unload();

        Assert.Empty(f.Commands);
        Assert.Empty(f.Adapter.Overrides);
        Assert.Empty(f.Adapter.PendingConVars);
    }

    [Fact]
    public void QueuedCommandsAreNotReportedAsAppliedAndRepeatedPolicyDoesNotDuplicateThem()
    {
        var f = new Fixture();
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);

        Assert.Equal(5, f.Commands.Count);
        Assert.All(f.Values.Values, cvar => Assert.Equal(0, cvar.Writes));
        Assert.Empty(f.Adapter.Overrides);
        Assert.Equal(5, f.Adapter.PendingConVars.Count);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(EngineStateDiagnostics.Capture(f.Core, f.Adapter)));
        Assert.Empty(json.RootElement.GetProperty("Overrides").EnumerateObject());
        Assert.Equal(5, json.RootElement.GetProperty("PendingConVars").EnumerateObject().Count());

        f.ExecuteQueuedCommands();

        Assert.Equal(5, f.Adapter.Overrides.Count);
        Assert.Empty(f.Adapter.PendingConVars);
        Assert.Equal("false", f.Adapter.Overrides["mp_match_end_restart"].Applied);
    }

    [Fact]
    public void UnloadBeforeTheCommandBufferRunsQueuesRestorationAfterApplication()
    {
        var f = new Fixture();
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        f.Adapter.Dispose();
        f.Adapter.Dispose();
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);

        Assert.Equal(10, f.Commands.Count);
        f.ExecuteQueuedCommands();

        Assert.Equal("45.000000", f.Time.Value);
        Assert.Equal("24", f.Rounds.Value);
        Assert.Equal("13", f.Wins.Value);
        Assert.Equal("true", f.ChangeLevel.Value);
        Assert.Equal("true", f.Restart.Value);
        Assert.Empty(f.Adapter.Overrides);
        Assert.Empty(f.Adapter.PendingConVars);
    }

    [Fact]
    public void PendingApplicationDoesNotLoseAnExternalSettingOnUnload()
    {
        var f = new Fixture();
        f.Adapter.ApplyRotationPolicy(rotationEnabled: false);
        f.Time.Value = "12.500000";
        f.Adapter.Dispose();
        f.ExecuteQueuedCommands();

        Assert.Equal("12.500000", f.Time.Value);
        Assert.Equal("24", f.Rounds.Value);
    }

    [Fact]
    public void ClearingThePoolBeforeApplicationLeavesOnlyTheUnlimitedLimits()
    {
        var f = new Fixture();
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        f.Adapter.ApplyRotationPolicy(rotationEnabled: false);
        f.ExecuteQueuedCommands();

        Assert.Equal("0", f.Rounds.Value);
        Assert.Equal("true", f.ChangeLevel.Value);
        Assert.Equal("true", f.Restart.Value);
        Assert.Equal(3, f.Adapter.Overrides.Count);
        Assert.Empty(f.Adapter.PendingConVars);
    }

    [Fact]
    public void ReactivatingWhileRestorationIsQueuedPreservesTheOriginalRestorePoint()
    {
        var f = new Fixture();
        f.ApplyPolicy(rotationEnabled: true);
        f.Adapter.ApplyRotationPolicy(rotationEnabled: false);
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        f.ExecuteQueuedCommands();

        Assert.Equal("false", f.ChangeLevel.Value);
        Assert.Equal("false", f.Restart.Value);
        Assert.Equal(5, f.Adapter.Overrides.Count);
        f.Unload();
        Assert.Equal("true", f.ChangeLevel.Value);
        Assert.Equal("true", f.Restart.Value);
    }

    [Fact]
    public void MapConfigThatRunsBeforeConfirmationIsAppliedAgainOnMapLoad()
    {
        var f = new Fixture();
        f.Adapter.ApplyRotationPolicy(rotationEnabled: false);
        // Движок исполнил команду, но конфиг новой карты уже вернул исходный лимит.
        f.CommandBuffer.Clear();
        f.ApplyPolicy(rotationEnabled: false, mapLoaded: true);

        Assert.Equal("0", f.Rounds.Value);
        Assert.Equal(3, f.Adapter.Overrides.Count);
        f.Unload();
        Assert.Equal("24", f.Rounds.Value);
    }

    [Fact]
    public void RejectedCommandsDoNotAcquireOwnershipOrOverwriteLaterSettingsOnUnload()
    {
        var f = new Fixture();
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        f.CommandBuffer.Clear();
        f.Clock.Advance(TimeSpan.FromSeconds(5));
        f.Adapter.ObservePendingChanges();
        f.Rounds.Value = "40";
        f.Unload();

        Assert.Equal(5, f.Commands.Count);
        Assert.Equal("40", f.Rounds.Value);
        Assert.Empty(f.Adapter.PendingConVars);
        Assert.Empty(f.Adapter.Overrides);
    }

    [Theory]
    [InlineData("mp_timelimit", "12.500000", "mp_timelimit 12.5\n")]
    [InlineData("mp_maxrounds", "30", "mp_maxrounds 30\n")]
    [InlineData("mp_match_end_restart", "false", "mp_match_end_restart 0\n")]
    [InlineData("mp_match_end_changelevel", "true", "mp_match_end_changelevel 1\n")]
    public void ConsoleArgumentsUseInvariantNumbers(string name, string value, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            Assert.Equal(expected, MapEngineAdapter.ConVarCommand(name, value));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("mp_maxrounds", "0; quit")]
    [InlineData("mp_timelimit", "0\nquit")]
    [InlineData("mp_timelimit", "NaN")]
    [InlineData("mp_timelimit", "Infinity")]
    [InlineData("mp_match_end_restart", "2")]
    public void InvalidConsoleValuesAreRejected(string name, string value) =>
        Assert.Throws<FormatException>(() => MapEngineAdapter.ConVarCommand(name, value));

    [Fact]
    public void UnknownConsoleVariableIsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MapEngineAdapter.ConVarCommand("mp_restartgame", "1"));

    [Fact]
    public void DiagnosticsRemainReadOnlyAndPreserveAvailableSectionsAfterANativeReadFails()
    {
        var f = new Fixture();
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(EngineStateDiagnostics.Capture(f.Core, f.Adapter)));
        var engine = json.RootElement;

        Assert.True(engine.GetProperty("Globals").TryGetProperty("Error", out _));
        Assert.True(engine.GetProperty("Rules").TryGetProperty("Error", out _));
        Assert.Equal("true", engine.GetProperty("ConVars").GetProperty("mp_match_end_restart").GetString());
        Assert.Equal(JsonValueKind.Null, engine.GetProperty("ConVars").GetProperty("host_timescale").ValueKind);
        Assert.Empty(engine.GetProperty("Overrides").EnumerateObject());
        Assert.All(f.Values.Values, cvar => Assert.Equal(0, cvar.Writes));
    }

    private sealed class Fixture
    {
        public FakeConVar Time { get; } = new("45.000000", typeof(float));
        public FakeConVar Rounds { get; } = new("24", typeof(int));
        public FakeConVar Wins { get; } = new("13", typeof(int));
        public FakeConVar ChangeLevel { get; } = new("true", typeof(bool));
        public FakeConVar Restart { get; } = new("true", typeof(bool));
        public Dictionary<string, FakeConVar> Values { get; }
        public ISwiftlyCore Core { get; }
        public MapEngineAdapter Adapter { get; }
        public TestClock Clock { get; } = new();
        public List<string> Commands { get; } = [];
        public Queue<string> CommandBuffer { get; } = new();

        public void ApplyPolicy(bool rotationEnabled, bool mapLoaded = false)
        {
            Adapter.ApplyRotationPolicy(rotationEnabled, mapLoaded);
            ExecuteQueuedCommands();
        }

        public void Unload()
        {
            Adapter.Dispose();
            ExecuteQueuedCommands();
        }

        public void ExecuteQueuedCommands()
        {
            while (CommandBuffer.TryDequeue(out var command))
            {
                Assert.EndsWith("\n", command);
                var parts = command.TrimEnd('\n').Split(' ');
                Assert.Equal(2, parts.Length);
                Values[parts[0]].ApplyConsoleValue(parts[1]);
            }
            Adapter.ObservePendingChanges();
        }

        public Fixture()
        {
            Values = new()
            {
                ["mp_timelimit"] = Time, ["mp_maxrounds"] = Rounds, ["mp_winlimit"] = Wins,
                ["mp_match_end_changelevel"] = ChangeLevel, ["mp_match_end_restart"] = Restart
            };
            var convars = Stub<IConVarService>((method, args) =>
            {
                var name = (string)args![0]!;
                var cvar = Values.GetValueOrDefault(name);
                return method.Name == nameof(IConVarService.FindAsString) ? cvar?.Proxy
                    : throw new InvalidOperationException("ConVar API разрешено только чтение: " + method.Name);
            });
            var engine = Stub<IEngineService>((method, args) =>
            {
                if (method.Name != nameof(IEngineService.ExecuteCommand)) throw new InvalidOperationException(method.Name);
                var command = (string)args![0]!;
                Commands.Add(command);
                CommandBuffer.Enqueue(command);
                return null;
            });
            Core = Stub<ISwiftlyCore>((method, _) => method.Name switch
            {
                "get_ConVar" => convars,
                "get_Engine" => engine,
                "get_Logger" => NullLogger.Instance,
                _ => throw new InvalidOperationException("Нативное состояние недоступно в тесте: " + method.Name)
            });
            Adapter = new(Core, Clock);
        }
    }

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan interval) => _now += interval;
    }

    private sealed class FakeConVar
    {
        public string Value { get; set; }
        public int Writes { get; private set; }
        public Type ValueType { get; }
        public IConVar Proxy { get; }

        public FakeConVar(string initial, Type valueType)
        {
            Value = initial;
            ValueType = valueType;
            Proxy = Stub<IConVar>((method, _) => method.Name == "get_ValueAsString" ? Value
                : throw new InvalidOperationException("Прямая запись ConVar запрещена: " + method.Name));
        }

        public void ApplyConsoleValue(string value)
        {
            Value = ValueType == typeof(float) ? float.Parse(value, CultureInfo.InvariantCulture).ToString("F6", CultureInfo.InvariantCulture)
                : ValueType == typeof(bool) ? value == "0" ? "false" : "true"
                : int.Parse(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            Writes++;
        }
    }

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceStub>();
        ((InterfaceStub)(object)proxy).Handler = handler;
        return proxy;
    }

    public class InterfaceStub : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }
}
