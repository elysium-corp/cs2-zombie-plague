using System.Reflection;
using Common.Database.Tasks;
using CustomHud.Api;
using Localization.Api;
using MapRotation.Api;
using MapRotation.Core.Database;
using MapRotation.Core.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class RotationHudLifecycleTests
{
    [Theory]
    [InlineData(HudMenuView.List)]
    [InlineData(HudMenuView.Compact)]
    [InlineData(HudMenuView.Result)]
    public void CatalogRefreshAppliesSpacingToExistingVoteAndResultWithoutResettingPersonalSettings(HudMenuView view)
    {
        using var f = new Fixture();
        f.Game.Engine.CastVote(1, f.Game.Engine.Vote!.Id, 2);
        f.Refresh();
        Assert.Equal(24, f.Menus.Menu!.VerticalGap);
        if (view == HudMenuView.Compact) f.Menus.Act(HudMenuAction.Close);
        if (view == HudMenuView.Result)
        {
            f.Game.Clock.Advance(20); f.Game.Engine.Tick(); f.Refresh();
        }
        var previous = f.Menus.Menu!;
        var id = f.Menus.Id;
        f.ApplyAppearance("{\"verticalGap\":32}");
        f.Refresh();
        var updated = f.Menus.Menu!;
        Assert.Equal(32, updated.VerticalGap);
        Assert.Equal(id, f.Menus.Id);
        Assert.Equal(1, f.Menus.OpenCount);
        Assert.Equal(view, updated.View);
        Assert.Equal(previous.Options.CaptureInput, updated.Options.CaptureInput);
        Assert.Equal(previous.Presentation, updated.Presentation);
        Assert.Equal<HudMenuItem>(previous.Items, updated.Items);
        Assert.Equal(previous.Participation, updated.Participation);
        if (view == HudMenuView.Result)
        {
            f.Game.Clock.Advance(5); f.Refresh(); Assert.True(f.Menus.Visible);
            f.Game.Clock.Advance(1); f.Refresh(); Assert.False(f.Menus.Visible);
        }
    }

    [Fact]
    public void CatalogRefreshUpdatesOpenNominationSpacingAndKeepsItsSelection()
    {
        using var f = new Fixture(startVote: false);
        Assert.Equal(RotationReply.Accepted, f.Game.Engine.Nominate(1, 2));
        f.Invoke("OpenNomination", f.Player);
        var id = f.Menus.Id;
        Assert.Equal(24, f.Menus.Menu!.VerticalGap);
        f.ApplyAppearance("{\"verticalGap\":8}");
        f.Refresh();
        Assert.Equal(8, f.Menus.Menu.VerticalGap);
        Assert.Equal(id, f.Menus.Id);
        Assert.Equal(1, f.Menus.OpenCount);
        Assert.True(f.Menus.Menu.Options.CaptureInput);
        Assert.Equal("2", Assert.Single(f.Menus.Menu.Items, item => item.Selected).Id);
    }

    [Fact]
    public void ClosingKeepsTheVotePassiveAndRtvReopensTheSameSession()
    {
        using var f = new Fixture();
        f.Refresh();
        var id = f.Menus.Id;
        f.Menus.Act(HudMenuAction.Close);
        Assert.Equal(HudMenuView.Compact, f.Menus.Menu!.View);
        Assert.False(f.Menus.Menu.Options.CaptureInput);
        f.Game.Clock.Advance(1); f.Refresh();
        Assert.Equal(HudMenuView.Compact, f.Menus.Menu.View);
        Assert.Equal("00:19", f.Menus.Menu.Status);
        f.Invoke("OpenVote", f.Player, true);
        Assert.Equal(HudMenuView.List, f.Menus.Menu.View);
        Assert.True(f.Menus.Menu.Options.CaptureInput);
        Assert.Equal(id, f.Menus.Id);
        Assert.Equal(1, f.Menus.OpenCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompletionReplacesFullOrCompactVoteWithFrozenPassiveResultAndThenHides(bool compact)
    {
        using var f = new Fixture();
        f.Refresh();
        var id = f.Menus.Id;
        if (compact) f.Menus.Act(HudMenuAction.Close);
        f.Game.Engine.CastVote(1, f.Game.Engine.Vote!.Id, 2);
        f.Game.Clock.Advance(20); f.Game.Engine.Tick();
        f.Game.Engine.SetPlayers([1]);
        f.Refresh();
        Assert.Equal(id, f.Menus.Id);
        Assert.Equal(1, f.Menus.OpenCount);
        Assert.Equal(HudMenuView.Result, f.Menus.Menu!.View);
        Assert.False(f.Menus.Menu.Options.CaptureInput);
        Assert.False(f.Menus.Menu.Options.Modal);
        Assert.False(f.Menus.Menu.Options.Closable);
        Assert.Equal("1/4", f.Menus.Menu.Participation);
        Assert.Equal("de_map2", Assert.Single(f.Menus.Menu.Items).Description);
        Assert.Equal(HudMenuDockSide.Left, f.Menus.Menu.Presentation.DockSide);
        f.Game.Clock.Advance(5); f.Refresh(); Assert.True(f.Menus.Visible);
        f.Game.Clock.Advance(1); f.Refresh(); Assert.False(f.Menus.Visible);
    }

    [Fact]
    public void CatalogRefreshSwitchesThemeClassesOnTheOpenVoteWithoutReopening()
    {
        using var f = new Fixture();
        f.Refresh();
        var id = f.Menus.Id;
        Assert.Empty(f.Menus.Menu!.ThemeClasses);
        f.ApplyAppearance("{\"animation\":\"pop\",\"duration\":300,\"radius\":4}");
        f.Refresh();
        Assert.Equal(["ThemeEntrancePop", "ThemeDuration300", "ThemeRadius4"], f.Menus.Menu!.ThemeClasses.ToArray());
        Assert.Equal(id, f.Menus.Id);
        Assert.Equal(1, f.Menus.OpenCount);
    }

    [Fact]
    public void HidingTheVoteRemovesItUntilRtvWhileTheResultStillAppears()
    {
        using var f = new Fixture();
        f.Refresh();
        f.ApplyAppearance("{\"voteClose\":\"hide\",\"horizontalGap\":8}");
        f.Refresh();
        Assert.False(f.Menus.Menu!.Options.CollapseOnClose);
        Assert.Equal(8, f.Menus.Menu.HorizontalGap);
        f.Menus.Act(HudMenuAction.Close);
        Assert.False(f.Menus.Visible);
        f.Game.Clock.Advance(1); f.Refresh();
        Assert.False(f.Menus.Visible);
        Assert.Equal(1, f.Menus.OpenCount);
        f.Invoke("OpenVote", f.Player, true);
        Assert.True(f.Menus.Visible);
        Assert.Equal(HudMenuView.List, f.Menus.Menu.View);
        Assert.Equal(2, f.Menus.OpenCount);
        f.Menus.Act(HudMenuAction.Close);
        f.Game.Engine.CastVote(1, f.Game.Engine.Vote!.Id, 2);
        f.Game.Clock.Advance(20); f.Game.Engine.Tick(); f.Refresh();
        Assert.True(f.Menus.Visible);
        Assert.Equal(HudMenuView.Result, f.Menus.Menu.View);
        Assert.Equal(8, f.Menus.Menu.HorizontalGap);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisabledResultClosesTheVoteAndAnnouncesTheNextMapInChat(bool compact)
    {
        using var f = new Fixture();
        f.Refresh();
        f.ApplyAppearance("{\"showResult\":false}");
        f.Refresh();
        if (compact) f.Menus.Act(HudMenuAction.Close);
        f.Game.Engine.CastVote(1, f.Game.Engine.Vote!.Id, 2);
        f.Game.Clock.Advance(20); f.Game.Engine.Tick(); f.Refresh();
        Assert.False(f.Menus.Visible);
        Assert.NotEqual(HudMenuView.Result, f.Menus.Menu!.View);
        Assert.Equal(1, f.Menus.OpenCount);
        Assert.Equal("MapRotation.CardSummary", Assert.Single(f.Messages));
    }

    [Fact]
    public void CancellingAVoteClosesThePanelWithoutCreatingAWinner()
    {
        using var f = new Fixture();
        f.Refresh();
        f.Menus.Act(HudMenuAction.Close);
        Assert.True(f.Game.Engine.SetNext(3, false));
        f.Refresh();
        Assert.False(f.Menus.Visible);
        Assert.Equal(1, f.Menus.OpenCount);
    }

    private sealed class Fixture : IDisposable
    {
        public RotationEngineTests.Fixture Game { get; } = new();
        public Menus Menus { get; } = new();
        public List<string> Messages { get; } = [];
        public IPlayer Player { get; }
        private readonly DatabaseTaskTracker _tasks = new(NullLogger<DatabaseTaskTracker>.Instance);
        private readonly RotationHudPreferences _preferences;
        private readonly RotationStore _store = new(null!, NullLogger<RotationStore>.Instance, Path.GetTempPath());
        private readonly RotationCoordinator _coordinator;

        public Fixture(bool startVote = true)
        {
            Player = (IPlayer)Proxy(typeof(IPlayer), (method, args) =>
            {
                if (method.Name == "SendMessage") { Messages.Add((string)args![1]!); return null; }
                return method.Name switch
                {
                    "get_IsValid" => true, "get_IsFakeClient" => false, "get_PlayerID" => 1,
                    "get_SteamID" => 1UL, "get_SessionId" => 10UL,
                    _ => throw new InvalidOperationException(method.Name)
                };
            });
            var core = (ISwiftlyCore)Proxy(typeof(ISwiftlyCore), (method, _) => method.Name switch
            {
                "get_PlayerManager" => Proxy(method.ReturnType, (member, _) => member.Name switch
                {
                    "GetPlayer" => Player,
                    "GetAllPlayers" => new[] { Player },
                    _ => throw new InvalidOperationException(member.Name)
                }),
                "get_Engine" => Proxy(method.ReturnType, (member, _) => member.Name switch
                {
                    "IsMapValid" => true,
                    _ => throw new InvalidOperationException(member.Name)
                }),
                _ => throw new InvalidOperationException(method.Name)
            });
            var localization = (ILocalizationApi)Proxy(typeof(ILocalizationApi), (method, args) =>
            {
                if (method.Name == nameof(ILocalizationApi.GetTagForPlayer)) return null;
                if (method.Name != nameof(ILocalizationApi.FormatForPlayer)) throw new InvalidOperationException(method.Name);
                var key = (string)args![1]!;
                var values = (IReadOnlyDictionary<string, object?>)args[2]!;
                return key == "MapRotation.Participation" ? $"{values["voted"]}/{values["total"]}" : key;
            });
            _preferences = new(new PreferenceStore(), new(), _tasks);
            _preferences.ConfigureDefaults(RotationHudPreferences.Default with { DockSide = HudMenuDockSide.Left });
            _coordinator = new(core, Game.Engine, _store, new(core), Game.Clock, new(() => localization), _preferences);
            _coordinator.Bind(Menus, null, null);
            if (startVote) Assert.True(Game.Engine.StartVote(NextMapSource.Admin));
            typeof(RotationCoordinator).GetField("_loaded", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_coordinator, true);
            typeof(RotationCoordinator).GetField("_lastVote", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(_coordinator, Game.Engine.Vote?.Id);
            Game.Engine.VoteFinished += result => Invoke("OnVoteFinished", result);
        }

        public void Refresh() => Invoke("RefreshHud");
        public void ApplyAppearance(string json) => Invoke("Apply", Game.Engine.Configuration with
        {
            Settings = Game.Engine.Settings with { HudSettings = json }
        });
        public void Invoke(string method, params object?[] args) => typeof(RotationCoordinator)
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_coordinator, args);
        public void Dispose() { _preferences.Dispose(); _tasks.Dispose(); _store.Dispose(); }
    }

    private sealed class PreferenceStore : IRotationHudPreferenceStore
    {
        public Task<HudMenuPresentation?> LoadAsync(ulong steamId, CancellationToken cancellationToken) => Task.FromResult<HudMenuPresentation?>(null);
        public Task SaveAsync(ulong steamId, HudMenuPresentation presentation, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Menus : ICustomHudMenuApi
    {
        public event Action<IPlayer>? Opening { add { } remove { } }
        public Guid Id { get; private set; }
        public HudMenu? Menu { get; private set; }
        public int OpenCount { get; private set; }
        public bool Visible { get; private set; }
        private Action<HudMenuEvent>? _handler;
        public bool IsAnyOpen(IPlayer player) => Visible && Menu?.Options.CaptureInput == true;
        public Guid? Open(IPlayer player, HudMenu menu, Action<HudMenuEvent> onAction)
        {
            OpenCount++; Id = Guid.NewGuid(); Menu = menu; Visible = true; _handler = onAction; return Id;
        }
        public bool Update(IPlayer player, Guid menuId, HudMenu menu)
        {
            if (!Visible || menuId != Id || menu.Channel != Menu?.Channel) return false;
            Menu = menu; return true;
        }
        public void Close(IPlayer player, Guid menuId) { if (menuId == Id) Visible = false; }
        public bool IsOpen(IPlayer player, Guid menuId) => Visible && Id == menuId;
        public void CloseChannel(string channel) { if (Menu?.Channel == channel) Visible = false; }
        public void Act(HudMenuAction action) => _handler!(new(Id, action, null));
    }

    private static object Proxy(Type type, Func<MethodInfo, object?[]?, object?> handler)
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
