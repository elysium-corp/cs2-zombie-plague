using System.Reflection;
using Common.Database.Storages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using Xunit;
using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Entities;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Data.Entities.Zombie.Classes;
using ZombiePlague.Core.Hud.AbilityHud;
using ZombiePlague.Core.Store.Data;
using RoleManager = ZombiePlague.Core.Data.Managers.Contracts.IPlayerManager;

namespace ZombiePlague.Core.Tests;

public sealed class AbilityHudLifecycleTests
{
    [Fact]
    public void DefaultConfigurationStartsOnceAndRendersWhileAnOrdinaryMenuIsOpen()
    {
        using var fixture = new Fixture();
        fixture.Service.Start();
        fixture.Service.Start();
        Assert.Single(fixture.WorldUpdates);
        fixture.FlushWorldUpdates();
        fixture.Tick();
        Assert.True(fixture.Service.IsRunning);
        Assert.All(fixture.Players, player => Assert.True(fixture.Current.IsShown(player.PlayerID)));
        Assert.Single(fixture.Timers);
    }

    [Fact]
    public void LegacyHideWhenMenuOpenIsIgnoredAndExplicitDisableStillWorks()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Enabled"] = "false", ["HideWhenMenuOpen"] = "true", ["ShowNames"] = "false"
        }).Build().Get<AbilityHudConfig>()!;
        using var fixture = new Fixture(configuration);
        fixture.Service.Start();
        Assert.Empty(fixture.WorldUpdates);
        fixture.Command("on");
        fixture.FlushWorldUpdates();
        fixture.Tick();
        Assert.True(fixture.Current.IsShown(1));
        fixture.MenuOpen = false;
        fixture.Tick();
        Assert.True(fixture.Current.IsShown(1));
    }

    [Fact]
    public void MissingResourcesDoNotLoseEnabledIntentAndNextMapRetries()
    {
        using var fixture = new Fixture { FactoryError = new FileNotFoundException("missing v4") };
        fixture.Start();
        Assert.False(fixture.Service.IsRunning);
        Assert.Contains(fixture.Command("status"), line => line.Contains("requested=True"));
        Assert.Empty(fixture.WorldUpdates);
        fixture.FactoryError = null;
        fixture.Raise("OnMapLoad", Stub<IOnMapLoadEvent>());
        fixture.FlushWorldUpdates();
        fixture.Tick();
        Assert.True(fixture.Current.IsShown(1));
    }

    [Fact]
    public void OffCancelsQueuedCreationAndStaysOffAcrossMaps()
    {
        using var fixture = new Fixture();
        fixture.Service.Start();
        fixture.Command("off");
        fixture.FlushWorldUpdates();
        fixture.Raise("OnMapLoad", Stub<IOnMapLoadEvent>());
        fixture.FlushWorldUpdates();
        Assert.Empty(fixture.Runtimes);
        Assert.False(fixture.Service.IsRunning);
    }

    [Fact]
    public void MapUnloadDisposesRuntimeAndOnWaitsUntilMapLoad()
    {
        using var fixture = new Fixture();
        fixture.Start();
        var previous = fixture.Current;
        var timer = fixture.Timers[0];
        fixture.Raise("OnMapUnload", Stub<IOnMapUnloadEvent>());
        Assert.True(previous.Disposed);
        Assert.True(timer.Token.IsCancellationRequested);
        fixture.Command("on");
        Assert.Empty(fixture.WorldUpdates);
        fixture.Raise("OnMapLoad", Stub<IOnMapLoadEvent>());
        fixture.FlushWorldUpdates();
        fixture.Tick();
        Assert.Equal(2, fixture.Runtimes.Count);
        Assert.True(fixture.Current.IsShown(1));
    }

    [Fact]
    public void StaleTimerCannotRenderIntoANewerRuntime()
    {
        using var fixture = new Fixture();
        fixture.Start();
        var oldTimer = fixture.Timers[0];
        fixture.Command("on");
        fixture.FlushWorldUpdates();
        oldTimer.Callback();
        Assert.Empty(fixture.Current.Calls);
        fixture.Tick();
        Assert.True(fixture.Current.IsShown(1));
    }

    [Fact]
    public void DeletedEntityIsRecreatedWithFullStateAndRepeatedDeletionIsBounded()
    {
        using var fixture = new Fixture();
        fixture.Start();
        for (var recovery = 0; recovery < 3; recovery++)
        {
            var previous = fixture.Current;
            previous.IsValid = false;
            fixture.Tick();
            fixture.FlushWorldUpdates();
            Assert.True(previous.Disposed);
            fixture.Tick();
            Assert.True(fixture.Current.IsShown(1));
        }
        fixture.Current.IsValid = false;
        fixture.Tick();
        Assert.False(fixture.Service.IsRunning);
        Assert.Empty(fixture.WorldUpdates);
        Assert.Equal(4, fixture.Runtimes.Count);
        fixture.Raise("OnMapLoad", Stub<IOnMapLoadEvent>());
        fixture.FlushWorldUpdates();
        fixture.Tick();
        Assert.True(fixture.Current.IsShown(1));
    }

    [Fact]
    public void BrokenPlayerFrameHidesOnlyThatPlayerAndRecoversWithoutLogSpam()
    {
        using var fixture = new Fixture();
        fixture.Start();
        fixture.Tick();
        fixture.BrokenPlayer = 1;
        fixture.Tick();
        fixture.Tick();
        Assert.True(fixture.Service.IsRunning);
        Assert.False(fixture.Current.IsShown(1));
        Assert.True(fixture.Current.IsShown(2));
        Assert.Equal(1, fixture.Logger.Warnings);
        fixture.BrokenPlayer = null;
        fixture.Tick();
        Assert.True(fixture.Current.IsShown(1));
    }

    [Fact]
    public void DisconnectClearsPlayerStateAndUnloadCancelsAllOwnedWork()
    {
        using var fixture = new Fixture();
        fixture.Start();
        fixture.Tick();
        fixture.Raise("OnClientDisconnected", Stub<IOnClientDisconnectedEvent>((_, _) => 1));
        Assert.False(fixture.Current.IsShown(1));
        Assert.True(fixture.Current.IsShown(2));
        fixture.Command("on");
        fixture.Service.Dispose();
        fixture.Service.Dispose();
        fixture.FlushWorldUpdates();
        Assert.All(fixture.Runtimes, runtime => Assert.True(runtime.Disposed));
        Assert.All(fixture.Timers, timer => Assert.True(timer.Token.IsCancellationRequested));
        Assert.Empty(fixture.Handlers);
        Assert.Equal(1, fixture.UnregisteredCommands);
    }

    private sealed class Fixture : IDisposable
    {
        public AbilityHudService Service { get; }
        public List<IPlayer> Players { get; } = [Player(1), Player(2)];
        public Queue<Action> WorldUpdates { get; } = new();
        public List<(Action Callback, CancellationToken Token)> Timers { get; } = [];
        public List<Runtime> Runtimes { get; } = [];
        public Dictionary<string, Delegate> Handlers { get; } = [];
        public TestLogger Logger { get; } = new();
        public Runtime Current => Runtimes[^1];
        public Exception? FactoryError { get; set; }
        public int? BrokenPlayer { get; set; }
        public bool MenuOpen { get; set; } = true;
        public int UnregisteredCommands { get; private set; }
        private ICommandService.CommandListener _command = null!;

        public Fixture(AbilityHudConfig? config = null)
        {
            var document = ZombieCatalogDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "zombie_catalog.example.json")));
            var roles = Players.ToDictionary(player => player.PlayerID, player => (IPlayerRole)Zombie.Create(null!, player,
                new ZCatalogClass(document.Classes.Single(item => item.InternalName == "zombie_cleric"),
                    [new ProbeAbility { Presentation = new("heal", "Heal", "heal") }])));
            var manager = Stub<RoleManager>((method, args) =>
            {
                if (method.Name == "GetAllPlayers") return Players.ToArray();
                if (method.Name == "TryGetRole")
                {
                    var id = ((IPlayer)args![0]!).PlayerID;
                    if (id == BrokenPlayer) throw new InvalidDataException("broken role");
                    args[1] = roles[id];
                    return true;
                }
                throw new InvalidOperationException(method.Name);
            });
            var core = Stub<ISwiftlyCore>((method, _) => method.Name switch
            {
                "get_Logger" => Logger,
                "get_PlayerManager" => Stub(method.ReturnType, (_, _) => Players),
                "get_MenusAPI" => Stub<IMenuManagerAPI>((_, _) => MenuOpen ? Stub<IMenuAPI>() : null),
                "get_Command" => Stub<ICommandService>((member, args) =>
                {
                    if (member.Name == "RegisterCommand")
                    {
                        _command = (ICommandService.CommandListener)args![1]!;
                        return Guid.NewGuid();
                    }
                    if (member.Name == "UnregisterCommand") { UnregisteredCommands++; return null; }
                    throw new InvalidOperationException(member.Name);
                }),
                "get_Event" => Stub(method.ReturnType, (member, args) =>
                {
                    var key = member.Name[(member.Name.IndexOf('_') + 1)..];
                    if (member.Name.StartsWith("add_", StringComparison.Ordinal)) Handlers.Add(key, (Delegate)args![0]!);
                    else if (member.Name.StartsWith("remove_", StringComparison.Ordinal)) Assert.True(Handlers.Remove(key));
                    else throw new InvalidOperationException(member.Name);
                    return null;
                }),
                "get_Scheduler" => Stub(method.ReturnType, (member, args) =>
                {
                    if (member.Name == "NextWorldUpdate") { WorldUpdates.Enqueue((Action)args![0]!); return null; }
                    if (member.Name == "DelayAndRepeatBySeconds")
                    {
                        var timer = new CancellationTokenSource();
                        Timers.Add(((Action)args![2]!, timer.Token));
                        return timer;
                    }
                    throw new InvalidOperationException(member.Name);
                }),
                _ => throw new InvalidOperationException(method.Name)
            });
            Service = new(core, manager, Options.Create(config ?? new AbilityHudConfig { ShowNames = false }),
                () => throw new InvalidOperationException("Подписи выключены"),
                new AbilityHudSettings(new PlayerSessionStore<PlayerPreferences>()), () =>
                {
                    if (FactoryError is not null) throw FactoryError;
                    var runtime = new Runtime();
                    Runtimes.Add(runtime);
                    return runtime;
                });
        }

        public void Start() { Service.Start(); FlushWorldUpdates(); }
        public void Tick() => Timers[^1].Callback();
        public void FlushWorldUpdates() { while (WorldUpdates.TryDequeue(out var callback)) callback(); }
        public void Raise(string name, object args) => Handlers[name].DynamicInvoke(args);
        public List<string> Command(string command)
        {
            var replies = new List<string>();
            _command(Stub<ICommandContext>((method, args) =>
            {
                if (method.Name == "Reply") { replies.Add((string)args![0]!); return null; }
                return method.Name switch { "get_Args" => new[] { command }, "get_IsSentByPlayer" => false, _ => null };
            }));
            return replies;
        }
        public void Dispose() => Service.Dispose();
    }

    private sealed class Runtime : IAbilityHudRuntime
    {
        public bool IsValid { get; set; } = true;
        public bool Disposed { get; private set; }
        public List<(int Player, string Panel, string Name, bool Enabled)> Calls { get; } = [];
        public bool IsShown(int player) => Calls.LastOrDefault(call => call.Player == player
            && call.Panel == "AbilityBuffs" && call.Name == "Shown").Enabled;
        public void SetClass(int playerId, string panel, string name, bool enabled)
        {
            Assert.False(Disposed);
            Assert.True(IsValid);
            Calls.Add((playerId, panel, name, enabled));
        }
        public void SetText(int playerId, string panel, string value) { Assert.False(Disposed); Assert.True(IsValid); }
        public void Dispose() { Assert.False(Disposed); Disposed = true; }
    }

    private sealed class TestLogger : ILogger
    {
        public int Warnings { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) { if (logLevel >= LogLevel.Warning) Warnings++; }
    }

    private sealed class ProbeAbility() : BaseActiveAbility(null!, new HealConfig(), () => throw new InvalidOperationException())
    {
        public override KeyKind? Key => KeyKind.E;
        public override float Cooldown => 10;
        public override void Use() => throw new InvalidOperationException("HUD не должен активировать способности");
    }

    private static IPlayer Player(int id) => Stub<IPlayer>((method, _) => method.Name switch
    {
        "get_IsValid" or "get_IsAlive" => true,
        "get_IsFakeClient" => false,
        "get_PlayerID" => id,
        "get_SteamID" => 76561198000000000UL + (ulong)id,
        _ => throw new InvalidOperationException(method.Name)
    });

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?>? handler = null) where T : class =>
        (T)Stub(typeof(T), handler ?? ((_, _) => null));
    private static object Stub(Type type, Func<MethodInfo, object?[]?, object?> handler)
    {
        var proxy = DispatchProxy.Create(type, typeof(AbilityHudDiagnosticsTests.InterfaceStub));
        ((AbilityHudDiagnosticsTests.InterfaceStub)proxy).Handler = handler;
        return proxy;
    }
}
