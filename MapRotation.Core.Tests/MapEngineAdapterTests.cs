using System.Globalization;
using System.Reflection;
using System.Text.Json;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Convars;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class MapEngineAdapterTests
{
    [Fact]
    public void EmptyPoolKeepsUnlimitedLimitsWithoutSuppressingNativeMatchExit()
    {
        var f = new Fixture();

        f.Adapter.ApplyRotationPolicy(rotationEnabled: false);

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
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        Assert.Equal("false", f.ChangeLevel.Value);
        Assert.Equal("false", f.Restart.Value);

        f.Adapter.ApplyRotationPolicy(rotationEnabled: false);
        f.Adapter.ApplyRotationPolicy(rotationEnabled: false);

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
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        for (var i = 0; i < 20; i++) f.Adapter.ApplyRotationPolicy(rotationEnabled: true);

        Assert.All(f.Values.Values, cvar => Assert.Equal(1, cvar.Writes));
        Assert.Equal("false", f.Adapter.Overrides["mp_match_end_restart"].Applied);
        Assert.Equal("0.000000", f.Adapter.Overrides["mp_timelimit"].Applied);
    }

    [Fact]
    public void UnloadRestoresCanonicalValuesAndIsIdempotent()
    {
        var f = new Fixture();
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);

        f.Adapter.Dispose();
        f.Adapter.Dispose();

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
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        f.Time.Value = "30.000000";
        f.Restart.Value = "true";

        f.Adapter.ApplyRotationPolicy(rotationEnabled: false);
        Assert.Equal(1, f.Restart.Writes);
        f.Time.Value = "20.000000";
        f.Adapter.Dispose();

        Assert.Equal("20.000000", f.Time.Value);
        Assert.Equal("true", f.Restart.Value);
        Assert.Equal(1, f.Restart.Writes);
    }

    [Fact]
    public void NewMapConfigurationBecomesTheNewRestorePoint()
    {
        var f = new Fixture();
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        f.Time.Value = "20.000000";
        f.Rounds.Value = "12";
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        f.Adapter.Dispose();

        Assert.Equal("20.000000", f.Time.Value);
        Assert.Equal("12", f.Rounds.Value);
    }

    [Fact]
    public void RefillingThePoolPreservesTheSettingsChosenWhileRotationWasInactive()
    {
        var f = new Fixture();
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        f.Adapter.ApplyRotationPolicy(rotationEnabled: false);
        f.Restart.Value = "false";
        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        f.Adapter.ApplyRotationPolicy(rotationEnabled: false);

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

        f.Adapter.ApplyRotationPolicy(rotationEnabled: true);
        f.Adapter.Dispose();

        Assert.All(f.Values.Values, cvar => Assert.Equal(0, cvar.Writes));
        Assert.Empty(f.Adapter.Overrides);
    }

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
        public FakeConVar Time { get; } = new("45.000000", value => double.Parse(value, CultureInfo.InvariantCulture).ToString("F6", CultureInfo.InvariantCulture));
        public FakeConVar Rounds { get; } = new("24");
        public FakeConVar Wins { get; } = new("13");
        public FakeConVar ChangeLevel { get; } = new("true", BooleanValue);
        public FakeConVar Restart { get; } = new("true", BooleanValue);
        public Dictionary<string, FakeConVar> Values { get; }
        public ISwiftlyCore Core { get; }
        public MapEngineAdapter Adapter { get; }

        public Fixture()
        {
            Values = new()
            {
                ["mp_timelimit"] = Time, ["mp_maxrounds"] = Rounds, ["mp_winlimit"] = Wins,
                ["mp_match_end_changelevel"] = ChangeLevel, ["mp_match_end_restart"] = Restart
            };
            var convars = Stub<IConVarService>((method, args) => method.Name == nameof(IConVarService.FindAsString)
                ? Values.GetValueOrDefault((string)args![0]!)?.Proxy
                : throw new InvalidOperationException(method.Name));
            Core = Stub<ISwiftlyCore>((method, _) => method.Name == "get_ConVar"
                ? convars : throw new InvalidOperationException("Нативное состояние недоступно в тесте: " + method.Name));
            Adapter = new(Core);
        }

        private static string BooleanValue(string value) => value is "0" or "false" ? "false" : "true";
    }

    private sealed class FakeConVar
    {
        public string Value { get; set; }
        public int Writes { get; private set; }
        public IConVar Proxy { get; }

        public FakeConVar(string initial, Func<string, string>? canonicalize = null)
        {
            Value = initial;
            Proxy = Stub<IConVar>((method, args) =>
            {
                if (method.Name == "get_ValueAsString") return Value;
                if (method.Name != "set_ValueAsString") throw new InvalidOperationException(method.Name);
                var next = (string)args![0]!;
                Value = canonicalize?.Invoke(next) ?? next;
                Writes++;
                return null;
            });
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
