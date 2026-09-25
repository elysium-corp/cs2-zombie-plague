using System.Collections.Immutable;
using System.Reflection;
using System.Xml.Linq;
using CustomHud.Api;
using CustomHud.Core.Menus;
using Microsoft.Extensions.Logging.Abstractions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;

namespace CustomHud.Core.Tests;

public sealed class HudMenuTests
{
    [Fact]
    public void OpenUpdateCloseAndCaptureFollowTheSameConnection()
    {
        using var f = new Fixture(); var id = f.Open(); Assert.True(f.Current.Capture);
        Assert.True(f.Service.Update(f.Player, id, f.Menu with { Title = "Updated" }));
        Assert.Equal("Updated", f.Current.Texts["Title"]); Assert.True(f.Service.IsOpen(f.Player, id));
        f.Service.Close(f.Player, id); f.Service.Close(f.Player, id);
        Assert.False(f.Current.Capture); Assert.Equal(1, f.Current.DisposeCount); Assert.False(f.Service.IsOpen(f.Player, id));
    }
    [Fact]
    public void CriticalCannotBeDisplacedByNormalButCanBeUpdated()
    {
        using var f = new Fixture(); var id = f.Service.Open(f.Player, f.Menu with
            { Options = new() { Priority = HudMenuPriority.Critical } }, f.Actions.Add)!.Value;
        Assert.Null(f.Service.Open(f.Player, f.Menu, f.Actions.Add)); Assert.True(f.Service.IsOpen(f.Player, id));
        Assert.False(f.Service.Update(f.Player, id, f.Menu));
    }
    [Fact]
    public void OpeningNotifiesOtherFullscreenOwnersBeforeCapturingInput()
    {
        using var f = new Fixture(); var called = false;
        f.Service.Opening += player => { called = true; Assert.True(f.Service.IsAnyOpen(player)); Assert.Empty(f.Runtimes); };
        f.Open(); Assert.True(called); Assert.True(f.Current.Capture);
    }
    [Fact]
    public void PaginationRecreatesEntityAndRejectsOldPageClicks()
    {
        using var f = new Fixture(); f.Open(); var old = f.Current;
        f.Click("NextPage"); Assert.Equal("2 / 2", f.Current.Texts["Page"]); Assert.Equal(1, old.DisposeCount);
        f.Click("Item0", old.Entity); Assert.DoesNotContain(f.Actions, action => action.Action == HudMenuAction.Select);
        f.Click("Item0"); Assert.Equal("5", f.Actions[^1].ItemId);
        f.Click("NextPage"); Assert.Equal(2, f.Runtimes.Count);
        f.Click("PreviousPage"); Assert.Equal("1 / 2", f.Current.Texts["Page"]);
    }
    [Fact]
    public void ItemReorderRejectsClickSentForOldCatalog()
    {
        using var f = new Fixture(); var id = f.Open(); var old = f.Current;
        f.Service.Update(f.Player, id, f.Menu with { Items = f.Menu.Items.Reverse().ToImmutableArray() });
        f.Click("Item0", old.Entity); Assert.Empty(f.Actions);
        f.Click("Item0"); Assert.Equal("6", Assert.Single(f.Actions).ItemId);
    }
    [Fact]
    public void SelectedAndDisabledStatesRenderAndDisabledCannotBeSelected()
    {
        using var f = new Fixture();
        f.Menu = f.Menu with { Items = [new("a", "A", Enabled: false, DisabledReason: "Locked"), new("b", "B", Selected: true)] };
        f.Open(); Assert.True(f.Current.Classes[("Item0", "Disabled")]); Assert.True(f.Current.Classes[("Item1", "Selected")]);
        Assert.Equal("Locked", f.Current.Texts["Description0"]);
        f.Click("Item0"); Assert.Empty(f.Actions); f.Click("Item1"); Assert.Equal("b", Assert.Single(f.Actions).ItemId);
    }
    [Theory]
    [InlineData("Close")]
    [InlineData("disconnect")]
    [InlineData("map-unload")]
    [InlineData("plugin-unload")]
    [InlineData("error")]
    public void EveryLifecyclePathReleasesInput(string path)
    {
        using var f = new Fixture(); f.Open(); var runtime = f.Current;
        switch (path)
        {
            case "Close": f.Click("Close"); break;
            case "disconnect": f.Raise("OnClientDisconnected", Stub<IOnClientDisconnectedEvent>((_, _) => 1)); break;
            case "map-unload": f.Raise("OnMapUnload", Stub<IOnMapUnloadEvent>()); break;
            case "plugin-unload": f.Service.Dispose(); break;
            case "error": f.FailRender = true; f.Click("NextPage"); break;
        }
        Assert.False(runtime.Capture); Assert.Equal(1, runtime.DisposeCount); Assert.False(f.Service.IsAnyOpen(f.Player));
    }
    [Fact]
    public void ReconnectInvalidatesOldMenuIdAndOldPlayerObject()
    {
        using var f = new Fixture(); var oldPlayer = f.Player; var id = f.Open();
        f.Player = Player(2, 20); f.Raise("OnClientConnected", Stub<IOnClientConnectedEvent>((_, _) => 1));
        Assert.False(f.Service.IsOpen(f.Player, id)); var next = f.Open();
        f.Service.Close(oldPlayer, next); Assert.True(f.Service.IsOpen(f.Player, next));
        f.Click("Item0"); Assert.Equal(next, Assert.Single(f.Actions).MenuId);
    }
    [Fact]
    public void UnknownClicksAndClicksFromAnotherEntityAreIgnored()
    {
        using var f = new Fixture(); f.Open();
        f.Click("Item0", new Moq.Mock<CCSCustomHudLayout>().Object); f.Click("Item00"); f.Click("Item6"); f.Click("unknown");
        Assert.Empty(f.Actions);
    }
    [Fact]
    public void CloseOnSelectDisposesBeforeCallbackAndAllowsOpeningNewMenu()
    {
        using var f = new Fixture(); f.Menu = f.Menu with { Options = new() { CloseOnSelect = true } };
        var id = f.Service.Open(f.Player, f.Menu, action =>
        {
            Assert.False(f.Service.IsAnyOpen(f.Player)); Assert.False(f.Current.Capture);
            Assert.NotNull(f.Service.Open(f.Player, f.Menu, _ => { }));
        });
        f.Click("Item0"); Assert.False(f.Service.IsOpen(f.Player, id!.Value)); Assert.True(f.Current.Capture);
    }
    [Fact]
    public void EscapeHonorsClosableAndBackRoutesSeparately()
    {
        using var f = new Fixture(); f.Menu = f.Menu with { ShowBack = true, Options = new() { Closable = false } }; f.Open();
        f.Raise("OnClientKeyStateChanged", Stub<IOnClientKeyStateChangedEvent>((method, _) => method.Name switch
        { "get_PlayerId" => 1, "get_Key" => KeyKind.Esc, "get_Pressed" => true, _ => null }));
        Assert.True(f.Service.IsAnyOpen(f.Player)); f.Click("Close"); Assert.Empty(f.Actions);
        f.Click("Back"); Assert.Equal(HudMenuAction.Back, Assert.Single(f.Actions).Action);
    }
    [Fact]
    public void ChannelClosePreservesOtherChannelsAndDisposeUnsubscribes()
    {
        var f = new Fixture(); var id = f.Open();
        f.Service.CloseChannel("Other"); Assert.True(f.Service.IsOpen(f.Player, id));
        f.Service.CloseChannel(f.Menu.Channel); Assert.False(f.Service.IsOpen(f.Player, id));
        f.Service.Dispose(); Assert.Empty(f.Handlers); Assert.True(f.Timer!.IsCancellationRequested);
    }
    [Fact]
    public void MissingMenuResourceDoesNotThrowOrDisableOtherSubsystems()
    {
        using var f = new Fixture { FailCreation = true };
        Assert.Null(f.Service.Open(f.Player, f.Menu, f.Actions.Add)); Assert.False(f.Service.IsAnyOpen(f.Player));
        f.FailCreation = false; Assert.NotNull(f.Service.Open(f.Player, f.Menu, f.Actions.Add));
    }
    [Fact]
    public void SharedLayoutHasSupportedPanelsUniqueIdsAndNoClientScript()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "menu-content/panorama/layout/custom_game/elysium_menu_v4.xml"));
        Assert.Null(document.Root!.Elements("Panel").Single().Attribute("id"));
        var ids = document.Descendants().Attributes("id").Select(attribute => attribute.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.All(document.Descendants(), element => Assert.Contains(element.Name.LocalName, new[] { "root", "styles", "include", "Panel", "Label", "Button", "Image" }));
        Assert.DoesNotContain(document.Descendants().Attributes(), attribute => attribute.Name.LocalName is "html" or "onactivate" or "onclick");
        Assert.Equal(new[] { "s2r://" + PanoramaMenuRuntime.Style, "s2r://" + PanoramaMenuRuntime.MapStyle }, document.Descendants("include").Select(x => x.Attribute("src")!.Value));
    }

    [Fact]
    public void ImageAndThemeChangesClearPreviousBindings()
    {
        using var f = new Fixture();
        var path = "panorama/images/custom_game/elysium/assets/" + new string('a', 64) + "_png.vtex";
        f.Menu = f.Menu with { StyleClass = "MapRotation", Items = f.Menu.Items.SetItem(0, f.Menu.Items[0] with { ImagePath = path }) };
        var id = f.Open();
        Assert.True(f.Current.Classes[("Image0", "HasImage")]);
        Assert.True(f.Current.Classes[("MenuRoot", "MapRotation")]);
        var imageClass = Assert.Single(f.Current.Classes.Where(pair => pair.Key.Item1 == "Image0" && pair.Key.Item2.StartsWith("MenuImage_custom_") && pair.Value)).Key.Item2;
        f.Menu = f.Menu with { StyleClass = "", Items = f.Menu.Items.SetItem(0, f.Menu.Items[0] with { ImagePath = null }) };
        Assert.True(f.Service.Update(f.Player, id, f.Menu));
        Assert.False(f.Current.Classes[("Image0", "HasImage")]);
        Assert.False(f.Current.Classes[("Image0", imageClass)]);
        Assert.False(f.Current.Classes[("MenuRoot", "MapRotation")]);
    }

    [Theory]
    [InlineData("https://example.com/image.png")]
    [InlineData("panorama/images/custom_game/elysium/assets/../../image.vtex")]
    public void InvalidImageCannotReplaceAnOpenMenu(string path)
    {
        using var f = new Fixture(); var id = f.Open();
        var menu = f.Menu with { Items = f.Menu.Items.SetItem(0, f.Menu.Items[0] with { ImagePath = path }) };
        Assert.Null(f.Service.Open(f.Player, menu, f.Actions.Add));
        Assert.True(f.Service.IsOpen(f.Player, id));
        Assert.True(f.Current.Capture);
    }

    [Fact]
    public void TenSlotsRenderAndTenthSlotSelectsItsOwnItem()
    {
        using var f = new Fixture();
        f.Menu = f.Menu with
        {
            Items = Enumerable.Range(0, 10).Select(i => new HudMenuItem(i.ToString(), "Map " + i)).ToImmutableArray(),
            Options = new() { ItemsPerPage = 10 },
            Presentation = new() { Orientation = HudMenuOrientation.Horizontal }
        };
        f.Open();
        Assert.True(f.Current.Classes[("MenuRoot", "PageItems10")]);
        Assert.True(f.Current.Classes[("MenuRoot", "HasSecondRow")]);
        Assert.False(f.Current.Classes[("Item9", "Hidden")]);
        Assert.True(f.Current.Classes[("Pagination", "Hidden")]);
        f.Click("Item9");
        Assert.Equal("9", Assert.Single(f.Actions).ItemId);
    }

    [Fact]
    public void SettingsPreservePageAndSelectedItemWithoutSubmittingSelection()
    {
        using var f = new Fixture();
        f.Menu = f.Menu with { SettingsText = SettingsLabels(), Items = f.Menu.Items.SetItem(5, f.Menu.Items[5] with { Selected = true }) };
        var id = f.Open();
        f.Click("NextPage"); f.Actions.Clear();
        var previous = f.Current;
        f.Click("Gear");
        f.Click("Item0", previous.Entity);
        f.Click("Item0"); f.Click("PreviousPage");
        Assert.Empty(f.Actions);
        f.Click("SetHorizontal"); f.Click("SetScale120");
        Assert.All(f.Actions, action => Assert.Equal(HudMenuAction.SettingsChanged, action.Action));
        var presentation = f.Actions[^1].Presentation!;
        Assert.Equal(HudMenuOrientation.Horizontal, presentation.Orientation);
        Assert.Equal(120, presentation.ScalePercent);
        Assert.True(f.Service.Update(f.Player, id, f.Menu with { Presentation = presentation, Status = "00:12" }));
        Assert.Equal("2 / 2", f.Current.Texts["Page"]);
        Assert.True(f.Current.Classes[("Item0", "Selected")]);
        Assert.True(f.Current.Classes[("MenuRoot", "SettingsOpen")]);
        f.Click("SettingsClose");
        Assert.False(f.Current.Classes[("MenuRoot", "SettingsOpen")]);
        f.Click("Item0");
        Assert.Equal("5", f.Actions[^1].ItemId);
    }

    [Fact]
    public void SettingsButtonsCannotChangeHiddenSettingsOrResultPresentation()
    {
        using var f = new Fixture(); f.Open();
        f.Click("SetHorizontal"); f.Click("Gear"); f.Click("SetScale120");
        Assert.Empty(f.Actions);
        f.Menu = f.Menu with { SettingsText = SettingsLabels(), ShowBrand = true, View = HudMenuView.Result };
        f.Open(); f.Click("Gear"); f.Click("SetScale80");
        Assert.Empty(f.Actions);
        Assert.False(f.Current.Classes[("MenuRoot", "HasBrand")]);
        Assert.False(f.Current.Classes[("MenuRoot", "HasSettings")]);
    }

    [Fact]
    public void UpdatingCatalogHidesUnneededPaginationAndSecondRow()
    {
        using var f = new Fixture();
        f.Menu = f.Menu with { Options = new() { ItemsPerPage = 10 } };
        var id = f.Open();
        Assert.True(f.Current.Classes[("MenuRoot", "HasSecondRow")]);
        f.Service.Update(f.Player, id, f.Menu with { Items = f.Menu.Items[..4] });
        Assert.True(f.Current.Classes[("MenuRoot", "PageItems4")]);
        Assert.False(f.Current.Classes[("MenuRoot", "HasSecondRow")]);
        Assert.True(f.Current.Classes[("Pagination", "Hidden")]);
        Assert.True(f.Current.Classes[("Row1", "Hidden")]);
        Assert.False(f.Current.Texts.ContainsKey("CloseText"));
    }

    private static HudMenuSettingsText SettingsLabels() => new()
    {
        Title = "Settings", Orientation = "Orientation", Horizontal = "Horizontal", Vertical = "Vertical",
        Size = "Size", Scale80 = "80%", Scale100 = "100%", Scale120 = "120%"
    };

    private sealed class Fixture : IDisposable
    {
        public Dictionary<string, Delegate> Handlers { get; } = [];
        public List<Runtime> Runtimes { get; } = [];
        public List<HudMenuEvent> Actions { get; } = [];
        public IPlayer Player { get; set; } = HudMenuTests.Player(1, 10);
        public HudMenu Menu { get; set; } = new("test", "Title", "Subtitle", Enumerable.Range(0, 7).Select(i => new HudMenuItem(i.ToString(), "Map " + i)).ToImmutableArray(), new());
        public Runtime Current => Runtimes[^1];
        public HudMenuService Service { get; }
        public CancellationTokenSource? Timer;
        public bool FailCreation;
        public bool FailRender;
        public Fixture()
        {
            var core = Stub<ISwiftlyCore>((method, _) => method.Name switch
            {
                "get_Logger" => NullLogger.Instance,
                "get_PlayerManager" => Proxy(method.ReturnType, (_, _) => Player),
                "get_Event" => Proxy(method.ReturnType, (member, args) =>
                {
                    var name = member.Name[(member.Name.IndexOf('_') + 1)..];
                    if (member.Name.StartsWith("add_")) Handlers.Add(name, (Delegate)args![0]!);
                    else Handlers.Remove(name); return null;
                }),
                "get_Scheduler" => Proxy(method.ReturnType, (_, _) => Timer = new()),
                _ => throw new InvalidOperationException(method.Name)
            });
            Service = new(core, _ =>
            {
                if (FailCreation) throw new FileNotFoundException();
                var runtime = new Runtime(() => FailRender); Runtimes.Add(runtime); return runtime;
            });
            Service.Start();
        }
        public Guid Open() => Service.Open(Player, Menu, Actions.Add)!.Value;
        public void Click(string button, CCSCustomHudLayout? entity = null)
            => Raise("OnCustomHudClicked", Stub<IOnCustomHudClickedEvent>((method, _) => method.Name switch
            { "get_PlayerId" => 1, "get_ButtonId" => button, "get_CustomHudLayout" => entity ?? Current.Entity, _ => null }));
        public void Raise(string name, object args) => Handlers[name].DynamicInvoke(args);
        public void Dispose() => Service.Dispose();
    }
    private sealed class Runtime(Func<bool> fail) : IHudMenuRuntime
    {
        public bool IsValid => DisposeCount == 0;
        public CCSCustomHudLayout Entity { get; } = new Moq.Mock<CCSCustomHudLayout>().Object;
        public bool Capture;
        public int DisposeCount;
        public Dictionary<string, string> Texts { get; } = [];
        public Dictionary<(string, string), bool> Classes { get; } = [];
        public bool Owns(CCSCustomHudLayout entity) => ReferenceEquals(entity, Entity);
        public void Text(string panel, string value) { if (fail()) throw new IOException(); Texts[panel] = value; }
        public void Class(string panel, string name, bool enabled) => Classes[(panel, name)] = enabled;
        public void Show(bool capture) => Capture = capture;
        public void Dispose() { DisposeCount++; Capture = false; }
    }
    private static IPlayer Player(ulong session, ulong steam) => Stub<IPlayer>((method, _) => method.Name switch
    {
        "get_IsValid" => true, "get_IsFakeClient" => false, "get_PlayerID" => 1,
        "get_SteamID" => steam, "get_SessionId" => session, _ => throw new InvalidOperationException(method.Name)
    });
    private static T Stub<T>(Func<MethodInfo, object?[]?, object?>? handler = null) where T : class => (T)Proxy(typeof(T), handler ?? ((_, _) => null));
    private static object Proxy(Type type, Func<MethodInfo, object?[]?, object?> handler)
    {
        var proxy = DispatchProxy.Create(type, typeof(InterfaceStub)); ((InterfaceStub)proxy).Handler = handler; return proxy;
    }
    public class InterfaceStub : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }
}
