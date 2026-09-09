using System.Reflection;
using System.Xml.Linq;
using CustomHud.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using Xunit;

namespace CustomHud.Core.Tests;

public sealed class HudRuntimeTests
{
    [Fact]
    public void ServiceBroadcastsWithoutMenuAccessAndOnlySendsChangedState()
    {
        using var fixture = new Fixture();
        fixture.Start();
        Assert.Equal(2, fixture.Service.Broadcast("<b>Начался раунд</b>"));
        fixture.Tick();
        Assert.True(fixture.Current.IsShown(1));
        Assert.True(fixture.Current.IsShown(2));
        var calls = fixture.Current.Calls;
        fixture.Tick();
        Assert.Equal(calls, fixture.Current.Calls);
        fixture.Clock.Advance(6);
        fixture.Tick();
        Assert.False(fixture.Current.IsShown(1));
    }

    [Fact]
    public void UnloadCancelsQueuedStartAndUnsubscribesAllCallbacks()
    {
        var fixture = new Fixture();
        fixture.Service.Start();
        fixture.Service.Dispose();
        fixture.Flush();
        Assert.Empty(fixture.Runtimes);
        Assert.Empty(fixture.Handlers);
        Assert.False(fixture.Service.IsAvailable);
        Assert.Equal(1, fixture.UnregisteredCommands);
    }

    [Fact]
    public void MapUnloadCancelsTimerClearsMessagesAndPreventsStaleCallbacks()
    {
        using var fixture = new Fixture();
        fixture.Start();
        fixture.Service.Broadcast("old map");
        fixture.Tick();
        var oldRuntime = fixture.Current;
        var oldTimer = fixture.Timers[0];
        fixture.Raise("OnMapUnload", Stub<IOnMapUnloadEvent>());
        Assert.True(oldTimer.Token.IsCancellationRequested);
        Assert.True(oldRuntime.Disposed);
        Assert.False(fixture.Service.Show(fixture.Players[0], "late"));
        fixture.Raise("OnMapLoad", Stub<IOnMapLoadEvent>());
        fixture.Flush();
        oldTimer.Callback();
        fixture.Tick();
        Assert.True(fixture.Service.IsAvailable);
        Assert.Equal(0, fixture.Current.Calls);
    }

    [Fact]
    public void MissingResourcesRetryOnNextMapWithoutFailingTheConsumer()
    {
        using var fixture = new Fixture { FailCreation = true };
        fixture.Start();
        Assert.False(fixture.Service.IsAvailable);
        Assert.Equal(0, fixture.Service.Broadcast("text"));
        fixture.FailCreation = false;
        fixture.Raise("OnMapLoad", Stub<IOnMapLoadEvent>());
        fixture.Flush();
        Assert.True(fixture.Service.IsAvailable);
    }

    [Fact]
    public void EntityRecoveryPreservesOnlyUnexpiredMessagesAndHasABoundedRetryCount()
    {
        using var fixture = new Fixture();
        fixture.Start();
        fixture.Service.Broadcast("recover");
        for (var attempt = 0; attempt < 3; attempt++)
        {
            fixture.Current.IsValid = false;
            fixture.Tick();
            fixture.Flush();
            fixture.Tick();
            Assert.True(fixture.Current.IsShown(1));
        }
        fixture.Current.IsValid = false;
        fixture.Tick();
        fixture.Flush();
        Assert.Equal(4, fixture.Runtimes.Count);
        Assert.False(fixture.Service.IsAvailable);
    }

    [Fact]
    public void DisconnectClearsEveryPanelForThatPlayerOnly()
    {
        using var fixture = new Fixture();
        fixture.Start();
        fixture.Service.Broadcast("text");
        fixture.Tick();
        fixture.Raise("OnClientDisconnected", Stub<IOnClientDisconnectedEvent>((_, _) => 1));
        fixture.Tick();
        Assert.False(fixture.Current.IsShown(1));
        Assert.True(fixture.Current.IsShown(2));
    }

    [Fact]
    public void SlotReuseClearsOtherPositionsBeforeDrawingNewPlayersMessage()
    {
        var runtime = new Runtime();
        var presenter = new HudPresenter(runtime);
        var document = HudMarkup.Parse("text", HudTextFormat.PlainText, HudMessageStyle.Notice);
        presenter.Render(1, 11, HudPosition.BottomRight, new(1, new(), document, 0));
        presenter.Render(1, 22, HudPosition.TopLeft, new(2, new(), document, 0));
        Assert.False(runtime.IsShown(1, HudPosition.BottomRight));
        Assert.True(runtime.IsShown(1, HudPosition.TopLeft));
    }

    [Fact]
    public void LocalizedBannerDelegatesTypedParametersToHtmlFormatterAndRejectsMissingFields()
    {
        using var fixture = new Fixture();
        fixture.Start();
        IReadOnlyDictionary<string, object?>? observed = null;
        fixture.Service.InitializeLocalization(Stub<Localization.Api.ILocalizationApi>((method, args) =>
        {
            if (method.Name != "FormatForPlayer") throw new InvalidOperationException(method.Name);
            Assert.Equal(Localization.Api.LocalizationOutputMode.Html, args![3]);
            observed = (IReadOnlyDictionary<string, object?>)args![2]!;
            return args[1] as string == "Missing" ? null : "<b>" + HudText.Escape((string)observed["player_name"]!) + "</b>";
        }));
        var parameters = new Dictionary<string, object?> { ["player_name"] = "<b>[red]Player", ["round"] = 7 };
        Assert.True(fixture.Service.ShowLocalized(fixture.Players[0], new(), new() { Title = "Title", Description = "Description" }, parameters));
        Assert.Equal("<b>[red]Player", observed!["player_name"]);
        Assert.Equal(7, observed["round"]);
        Assert.Equal("<b>[red]Player", parameters["player_name"]);
        Assert.False(fixture.Service.ShowLocalized(fixture.Players[0], new(), new() { Title = "Missing", Description = "Description" }, parameters));
    }

    [Fact]
    public void ResourcesRespectCustomHudWhitelistAndTheNetworkIdLimit()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "content/panorama");
        Assert.EndsWith("_v4_r2.vxml_c", PanoramaHudRuntime.Layout);
        Assert.EndsWith("_v4_r2.vcss_c", PanoramaHudRuntime.Style);
        var layout = XDocument.Load(Path.Combine(root, "layout/custom_game/elysium_messages_v4_r2.xml"));
        // Корневая панель не может иметь id: иначе Resource Compiler не создаёт vxml_c.
        Assert.Null(Assert.Single(layout.Root!.Elements("Panel")).Attribute("id"));
        Assert.Equal("s2r://" + PanoramaHudRuntime.Style, layout.Descendants("include").Single().Attribute("src")!.Value);
        var allowed = new Dictionary<string, string[]>
        {
            ["root"] = [], ["styles"] = [], ["include"] = ["src"],
            ["Image"] = ["id", "class", "hittest", "src", "texturewidth", "textureheight"],
            ["Panel"] = ["id", "class", "hittest"], ["Label"] = ["id", "class", "hittest", "text"]
        };
        foreach (var element in layout.Descendants())
        {
            Assert.True(allowed.TryGetValue(element.Name.LocalName, out var attributes), element.Name.LocalName);
            Assert.All(element.Attributes(), attribute => Assert.Contains(attribute.Name.LocalName, attributes!));
        }
        var ids = layout.Descendants().Attributes("id").Select(attribute => attribute.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.True(ids.Length < 1024);
        Assert.Contains("MessageRegion", ids);
        for (var lane = 0; lane < PanoramaHudRuntime.StackCapacity; lane++)
            for (var line = 0; line < HudMarkup.MaximumLines; line++)
                for (var run = 0; run < HudMarkup.MaximumRuns; run++)
                    Assert.Contains($"Message{lane}Line{line}Run{run}", ids);
        var css = File.ReadAllText(Path.Combine(root, "styles/custom_game/elysium_messages_v4_r2.css"));
        // Процентное ограничение сжимало баннер при расчёте размеров Custom HUD
        Assert.DoesNotContain("max-width: 28%", css);
        Assert.DoesNotContain("max-width: 38%", css);
        foreach (var width in new[] { 440, 600, 760 })
            Assert.Contains($"min-width: {width}px;", css);
        for (var color = 0; color < HudPalette.Colors.Length; color++)
            Assert.Contains($".MessageRun.C{color} {{ color: #{HudPalette.Colors[color]:X6}; }}", css);
        Assert.True(System.Text.RegularExpressions.Regex.Matches(css, @"\.([A-Za-z_][A-Za-z_0-9]*)")
            .Select(match => match.Groups[1].Value).Distinct().Count() < 1024);
        foreach (var src in layout.Descendants("Image").Attributes("src").Select(attribute => attribute.Value).Distinct())
        {
            Assert.StartsWith("s2r://panorama/images/", src);
            Assert.EndsWith(".vsvg", src);
            var svg = XDocument.Load(Path.Combine(root, src["s2r://panorama/".Length..].Replace(".vsvg", ".svg")));
            // Те же явные белые контуры, что у HUD способностей: без наследуемой заливки g и stroke.
            Assert.NotEmpty(svg.Root!.Elements());
            Assert.All(svg.Root.Elements(), path =>
            {
                Assert.Equal("path", path.Name.LocalName);
                Assert.Equal("#ffffff", path.Attribute("fill")?.Value, ignoreCase: true);
                Assert.False(string.IsNullOrWhiteSpace(path.Attribute("d")?.Value));
                Assert.All(path.Attributes(), attribute => Assert.Contains(attribute.Name.LocalName, new[] { "fill", "d" }));
            });
        }
    }

    [Fact]
    public void RegionAddressingAndPreflightCannotSilentlyLoseIcons()
    {
        Assert.Equal((1, "Message2TitleRun11"), PanoramaHudRuntime.Address("Message19TitleRun11"));
        Assert.Throws<ArgumentException>(() => PanoramaHudRuntime.Address("Message27Line0"));
        var missing = PanoramaHudRuntime.MissingResources(path => !path.EndsWith("/infection.vsvg_c", StringComparison.Ordinal));
        Assert.EndsWith("/infection.vsvg_c", Assert.Single(missing));
    }

    [Fact]
    public void StackedMessagesMoveWithoutReplayingSoundOrAppearanceAndClearOnDisconnect()
    {
        var clock = new HudMessageStoreTests.Clock(); var runtime = new Runtime();
        var store = new HudMessageStore(clock); var sounds = 0;
        var presenter = new HudPresenter(runtime, clock, (_, _, _) => sounds++);
        var document = HudBannerDesign.Parse(new() { Exit = "none", Sound = "Test.Sound" }, new() { Title = "Title", Description = "Text" }, HudTextFormat.Markup);
        store.Put(1, 11, document, new() { Channel = "event", Stack = true, DurationSeconds = 1 });
        store.Put(1, 11, document, new() { Channel = "event", Stack = true, DurationSeconds = 5 });
        presenter.RenderFrame(1, 11, store.GetStackedFrame(1, 11));
        Assert.Equal(2, sounds);
        clock.Advance(1); presenter.RenderFrame(1, 11, store.GetStackedFrame(1, 11));
        Assert.Equal(2, sounds); Assert.True(runtime.IsShown(1));
        Assert.False(runtime.Class(1, "Message10", "Shown"));
        Assert.False(runtime.Class(1, "Message1", "EntryA")); Assert.False(runtime.Class(1, "Message1", "EntryB"));
        presenter.Clear(1); Assert.False(runtime.IsShown(1));
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly Queue<Action> Updates = [];
        internal readonly List<(Action Callback, CancellationToken Token)> Timers = [];
        internal readonly Dictionary<string, Delegate> Handlers = [];
        internal readonly List<Runtime> Runtimes = [];
        internal readonly List<IPlayer> Players = [Player(1), Player(2)];
        internal readonly HudMessageStoreTests.Clock Clock = new();
        internal readonly CustomHudService Service;
        internal Runtime Current => Runtimes[^1];
        internal bool FailCreation;
        internal int UnregisteredCommands;

        internal Fixture()
        {
            var core = Stub<ISwiftlyCore>((method, _) => method.Name switch
            {
                "get_Logger" => NullLogger.Instance,
                "get_PlayerManager" => Stub(method.ReturnType, (_, _) => Players),
                "get_Command" => Stub<ICommandService>((member, _) =>
                {
                    if (member.Name == "RegisterCommand") return Guid.NewGuid();
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
                    if (member.Name == "NextWorldUpdate") { Updates.Enqueue((Action)args![0]!); return null; }
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
            Service = new(core, Options.Create(new CustomHudConfig()), Clock, () =>
            {
                if (FailCreation) throw new FileNotFoundException("VPK missing");
                var runtime = new Runtime();
                Runtimes.Add(runtime);
                return runtime;
            });
        }

        internal void Start() { Service.Start(); Flush(); }
        internal void Flush() { while (Updates.TryDequeue(out var callback)) callback(); }
        internal void Tick() => Timers[^1].Callback();
        internal void Raise(string name, object args) => Handlers[name].DynamicInvoke(args);
        public void Dispose() => Service.Dispose();
    }

    private sealed class Runtime : IHudRuntime
    {
        public bool IsValid { get; set; } = true;
        internal bool Disposed;
        internal int Calls;
        private readonly Dictionary<(int, string, string), bool> _classes = [];
        internal bool IsShown(int player, HudPosition position = HudPosition.TopCenter) =>
            _classes.GetValueOrDefault((player, "Message" + (int)position, "Shown"));
        internal bool Class(int player, string panel, string name) => _classes.GetValueOrDefault((player, panel, name));
        public void SetClass(int playerId, string panel, string name, bool enabled)
        {
            Assert.False(Disposed); Assert.True(IsValid); Calls++;
            _classes[(playerId, panel, name)] = enabled;
        }
        public void SetText(int playerId, string panel, string text) { Assert.False(Disposed); Assert.True(IsValid); Calls++; }
        public void Dispose() { Assert.False(Disposed); Disposed = true; }
    }

    private static IPlayer Player(int id) => Stub<IPlayer>((method, _) => method.Name switch
    {
        "get_IsValid" or "get_IsAuthorized" => true, "get_IsFakeClient" => false,
        "get_PlayerID" => id, "get_SteamID" => 76561198000000000UL + (ulong)id,
        _ => throw new InvalidOperationException(method.Name)
    });

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?>? handler = null) where T : class =>
        (T)Stub(typeof(T), handler ?? ((_, _) => null));
    private static object Stub(Type type, Func<MethodInfo, object?[]?, object?> handler)
    {
        var proxy = DispatchProxy.Create(type, typeof(InterfaceStub));
        ((InterfaceStub)proxy).Handler = handler;
        return proxy;
    }

    public class InterfaceStub : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }
}
