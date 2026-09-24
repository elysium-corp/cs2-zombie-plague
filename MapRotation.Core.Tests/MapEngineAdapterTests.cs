using System.Reflection;
using System.Text.Json;
using MapRotation.Core.Domain;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Convars;
using SwiftlyS2.Shared.Services;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class MapEngineAdapterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidatingAndChangingAMapDoesNotWriteMatchSettings(bool workshop)
    {
        var f = new Fixture();
        var map = new RotationEngineTests.Fixture().Maps[1] with { WorkshopId = workshop ? 3764581596L : null };
        f.Installed.Add(map.EngineTarget);

        Assert.True(f.Adapter.IsValid(map));
        Assert.Empty(f.Commands);
        f.Adapter.Change(map);
        Assert.Equal(workshop ? "host_workshop_map 3764581596" : "changelevel de_map2", Assert.Single(f.Commands));
    }

    [Fact]
    public void UnavailableOrDisabledMapsNeverReachTheCommandBuffer()
    {
        var f = new Fixture();
        var map = new RotationEngineTests.Fixture().Maps[1];
        Assert.Throws<InvalidOperationException>(() => f.Adapter.Change(map));
        f.Installed.Add(map.EngineTarget);
        Assert.Throws<InvalidOperationException>(() => f.Adapter.Change(map with { Enabled = false }));
        Assert.Empty(f.Commands);
    }

    [Fact]
    public void WorkshopInstalledOutsideEngineSearchPathRemainsAvailableForVotingAndChange()
    {
        var f = new Fixture();
        var rotation = new RotationEngineTests.Fixture();
        var map = rotation.Maps[1] with { MapName = "zm_gorodok", WorkshopId = 3100743780 };
        f.Workshop[3100743780] = new(4, "/steam-library/content/730/3100743780",
            "/steam-library/content/730/3100743780/3100743780.vpk", null);

        var check = f.Adapter.Inspect(map);
        Assert.True(check.IsValid);
        Assert.Equal("SteamUGC", check.Source);
        Assert.Equal("3100743780", check.EngineTarget);
        Assert.Empty(f.Commands);
        rotation.Engine.Configure(RotationConfiguration.Create(new(), [map]), [map.Id]);
        Assert.True(rotation.Engine.RotationEnabled);
        Assert.Null(Assert.Single(rotation.Engine.Catalog()).NominationExclusion);
        f.Adapter.Change(map);
        Assert.Equal("host_workshop_map 3100743780", Assert.Single(f.Commands));
    }

    [Fact]
    public void MountedWorkshopPathIsCheckedUsingTheSameIdAsTheChangeCommand()
    {
        var f = new Fixture();
        var map = new RotationEngineTests.Fixture().Maps[1] with
            { MapName = "zm_lila_hacker_meow_v3", WorkshopId = 3764581596 };
        f.Installed.Add("workshop/3764581596/zm_lila_hacker_meow_v3");

        Assert.Equal("WorkshopMapPath", f.Adapter.Inspect(map).Source);
        Assert.Empty(f.WorkshopChecks);
        f.Adapter.Change(map);
        Assert.Equal("host_workshop_map 3764581596", Assert.Single(f.Commands));
    }

    [Fact]
    public void AStockMapNameCannotValidateAnUnknownWorkshopId()
    {
        var f = new Fixture();
        var map = new RotationEngineTests.Fixture().Maps[1] with { WorkshopId = 3764581596 };
        f.Installed.Add(map.MapName);

        Assert.False(f.Adapter.IsValid(map));
        Assert.Throws<InvalidOperationException>(() => f.Adapter.Change(map));
        Assert.DoesNotContain(map.MapName, f.MapChecks);
        Assert.Empty(f.Commands);
    }

    [Fact]
    public void RemovingWorkshopFilesBeforeChangePreventsTheCommand()
    {
        var f = new Fixture();
        var map = new RotationEngineTests.Fixture().Maps[1] with { WorkshopId = 3764581596 };
        f.Workshop[3764581596] = new(4, "/library/map", "/library/map/map.vpk", null);
        Assert.True(f.Adapter.IsValid(map));
        f.Workshop.Clear();

        Assert.Throws<InvalidOperationException>(() => f.Adapter.Change(map));
        Assert.Empty(f.Commands);
    }

    [Fact]
    public void MissingWorkshopApiIsReportedWithoutAcceptingTheMap()
    {
        var f = new Fixture();
        var map = new RotationEngineTests.Fixture().Maps[1] with { WorkshopId = 3764581596 };
        f.Workshop[3764581596] = new(null, null, null, "InvalidOperationException");

        var check = f.Adapter.Inspect(map);
        Assert.False(check.IsValid);
        Assert.Equal("InvalidOperationException", check.Workshop!.ErrorType);
        Assert.Empty(f.Commands);
    }

    [Fact]
    public void UnsafeAndLocalMapsNeverQuerySteam()
    {
        var f = new Fixture();
        var map = new RotationEngineTests.Fixture().Maps[1];
        Assert.False(f.Adapter.IsValid(map));
        Assert.False(f.Adapter.IsValid(map with { MapName = "bad;quit", WorkshopId = 3764581596 }));
        Assert.Empty(f.WorkshopChecks);
    }

    [Fact]
    public void MissingPoolNeverSendsEngineCommandsAcrossReconnectsAndMatchEnd()
    {
        var f = new Fixture();
        var rotation = new RotationEngineTests.Fixture();
        rotation.Engine.Configure(RotationConfiguration.Empty, []);
        rotation.Engine.ChangeRequested += (map, _) => f.Adapter.Change(map);
        for (var i = 0; i < 3; i++)
        {
            rotation.Engine.SetPlayers([]);
            rotation.Clock.Advance(7200);
            rotation.Engine.Tick();
            rotation.Engine.MatchEnded();
            rotation.Engine.SetPlayers([1]);
            rotation.Engine.RoundEnded();
            rotation.Engine.Tick();
        }
        Assert.Empty(f.Commands);
        Assert.False(rotation.Engine.RotationEnabled);
    }

    [Fact]
    public void DiagnosticsRemainReadOnlyAfterANativeReadFails()
    {
        var f = new Fixture();
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(EngineStateDiagnostics.Capture(f.Core)));
        var engine = json.RootElement;
        Assert.True(engine.GetProperty("Globals").TryGetProperty("Error", out _));
        Assert.True(engine.GetProperty("Rules").TryGetProperty("Error", out _));
        Assert.Equal("true", engine.GetProperty("ConVars").GetProperty("sv_hibernate_when_empty").GetString());
        Assert.Equal("30", engine.GetProperty("ConVars").GetProperty("mp_maxrounds").GetString());
        Assert.Empty(engine.GetProperty("Overrides").EnumerateObject());
        Assert.Empty(engine.GetProperty("PendingConVars").EnumerateObject());
        Assert.Empty(f.Commands);
    }

    private sealed class Fixture
    {
        public ISwiftlyCore Core { get; }
        public MapEngineAdapter Adapter { get; }
        public List<string> Commands { get; } = [];
        public HashSet<string> Installed { get; } = [];
        public List<string> MapChecks { get; } = [];
        public Dictionary<long, WorkshopInstallation> Workshop { get; } = [];
        public List<long> WorkshopChecks { get; } = [];
        public Fixture()
        {
            var values = new Dictionary<string, string>
            {
                ["mp_maxrounds"] = "30", ["sv_hibernate_when_empty"] = "true"
            };
            var convars = Stub<IConVarService>((method, args) =>
            {
                if (method.Name != nameof(IConVarService.FindAsString))
                    throw new InvalidOperationException("Запись ConVar запрещена");
                return values.TryGetValue((string)args![0]!, out var value)
                    ? Stub<IConVar>((property, _) => property.Name == "get_ValueAsString" ? value
                        : throw new InvalidOperationException("Запись ConVar запрещена")) : null;
            });
            var engine = Stub<IEngineService>((method, args) =>
            {
                if (method.Name == nameof(IEngineService.IsMapValid))
                {
                    var name = (string)args![0]!;
                    MapChecks.Add(name);
                    return Installed.Contains(name);
                }
                if (method.Name != nameof(IEngineService.ExecuteCommand)) throw new InvalidOperationException(method.Name);
                Commands.Add((string)args![0]!);
                return null;
            });
            Core = Stub<ISwiftlyCore>((method, _) => method.Name switch
            {
                "get_ConVar" => convars,
                "get_Engine" => engine,
                _ => throw new InvalidOperationException("Нативное состояние недоступно в тесте: " + method.Name)
            });
            Adapter = new(Core, id =>
            {
                WorkshopChecks.Add(id);
                return Workshop.GetValueOrDefault(id) ?? new(0, null, null, null);
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
