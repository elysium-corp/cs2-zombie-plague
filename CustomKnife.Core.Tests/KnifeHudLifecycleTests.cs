using CustomKnife.Data.Knives;
using CustomKnife.Data.Menus;
using CustomKnife.Data.Models;
using CustomKnife.Data.Registrator;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Hud;
using CustomKnife.Services;
using Localization.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;
using ZombiePlague.Api;

namespace CustomKnife.Core.Tests;

public sealed class KnifeHudLifecycleTests
{
    [Fact]
    public void CmsImagesAreAppliedBeforeClassNamesAreRegisteredAndCanReturnToStock()
    {
        using var f = new Fixture();
        const string icon = "panorama/images/custom_game/elysium/equipment/katana.vsvg";
        const string preview = "panorama/images/custom_game/elysium/knife_previews/katana_png.vtex";
        f.Catalog[0] = KnifeDefaults.Fallback with { HudIconPath = icon, HudPreviewPath = preview };
        f.Core.Setup(core => core.GameFileSystem.FileExists(It.IsAny<string>(), "GAME")).Returns(true);
        Assert.True(f.Menu.Open(f.Player.Object));
        var hud = f.Huds.Single();
        hud.Verify(value => value.Choice("Image0", "image", KnifeHudAssets.CssClass(icon, false)), Times.Once);
        hud.Verify(value => value.Choice("Image0", "image", "Icon_knife"), Times.Never);
        hud.Verify(value => value.Choice("PreviewImage", "image", KnifeHudAssets.CssClass(preview, true)), Times.Once);
        hud.Verify(value => value.Choice(It.IsAny<string>(), "cmsImage", It.IsAny<string>()), Times.Never);
        hud.Verify(value => value.IsClassRegistered(It.IsAny<string>()), Times.Never);
        var status = f.Menu.AssetStatus().ToArray();
        foreach (var kind in new[] { "icon", "preview" })
            Assert.Contains(status, line => line.Contains("kind=" + kind) && line.Contains("resource=present")
                && line.Contains("image_source=cms") && line.Contains("class_table=unregistered"));

        f.Update!();
        hud.Verify(value => value.Choice("Image0", "image", "Icon_knife"), Times.Never);
        hud.Verify(value => value.Choice("PreviewImage", "image", "Preview_knife"), Times.Never);

        f.Click("Preview1");
        hud.Verify(value => value.Choice("PreviewImage", "image", "Preview_knife"), Times.Once);
    }

    [Fact]
    public void MissingCompiledResourceUsesStockAndDiagnosticsIdentifyTheFailedSide()
    {
        using var f = new Fixture();
        const string icon = "panorama/images/custom_game/elysium/equipment/katana.vsvg";
        const string preview = "panorama/images/custom_game/elysium/knife_previews/katana_png.vtex";
        f.Catalog[0] = KnifeDefaults.Fallback with { HudIconPath = icon, HudPreviewPath = preview };
        f.Core.Setup(core => core.GameFileSystem.FileExists(icon + "_c", "GAME")).Returns(true);
        Assert.True(f.Menu.Open(f.Player.Object));
        var hud = f.Huds.Single();
        hud.Verify(value => value.Choice("Image0", "image", KnifeHudAssets.CssClass(icon, false)), Times.Once);
        hud.Verify(value => value.Choice("PreviewImage", "image", "Preview_knife"), Times.Once);
        var status = f.Menu.AssetStatus().ToArray();
        Assert.Contains(status, line => line.Contains("kind=icon") && line.Contains("resource=present") && line.Contains("image_source=cms"));
        Assert.Contains(status, line => line.Contains("kind=preview") && line.Contains("resource=missing") && line.Contains("image_source=stock"));
        f.Core.Setup(core => core.GameFileSystem.FileExists(preview + "_c", "GAME")).Returns(true);
        f.Update!();
        hud.Verify(value => value.Choice("PreviewImage", "image", KnifeHudAssets.CssClass(preview, true)), Times.Once);
        Assert.Contains(f.Menu.AssetStatus(), line => line.Contains("kind=preview") && line.Contains("resource=present") && line.Contains("image_source=cms"));
    }

    [Fact]
    public void WrongEntityWrongConnectionAndRevokedPermissionCannotEquip()
    {
        using var f = new Fixture();
        Assert.True(f.Menu.Open(f.Player.Object));
        f.Click("Preview1");
        f.Owns = false;
        f.Click("Equip1");
        f.Owns = true;
        f.SessionId++;
        f.Click("Equip1");
        f.SessionId--;
        f.Allowed = false;
        f.Click("Equip1");
        f.Knives.Verify(service => service.SelectKnife(It.IsAny<IPlayer>(), It.IsAny<IKnife>()), Times.Never);
        f.Allowed = true;
        f.Click("Equip1");
        f.Knives.Verify(service => service.SelectKnife(f.Player.Object, f.Catalog[1]), Times.Once);
    }

    [Theory]
    [InlineData("disconnect")]
    [InlineData("map")]
    [InlineData("unload")]
    [InlineData("close")]
    [InlineData("infected")]
    [InlineData("team")]
    public void EveryClosePathReleasesRuntimeAndStopsRefresh(string cause)
    {
        using var f = new Fixture();
        f.Menu.Open(f.Player.Object);
        switch (cause)
        {
            case "disconnect":
                var disconnected = Mock.Of<IOnClientDisconnectedEvent>(ev => ev.PlayerId == 3);
                f.Core.Raise(core => core.Event.OnClientDisconnected += null, disconnected);
                break;
            case "map": f.Core.Raise(core => core.Event.OnMapUnload += null, Mock.Of<IOnMapUnloadEvent>()); break;
            case "unload": f.Menu.Dispose(); break;
            case "close": f.Click("Close"); break;
            case "infected": f.Infected = true; f.Update!(); break;
            case "team": f.Team = Team.Spectator; f.Update!(); break;
        }
        f.Huds[0].Verify(hud => hud.Dispose(), Times.Once);
        Assert.True(f.Timer!.IsCancellationRequested);
        f.Menu.Dispose();
        f.Huds[0].Verify(hud => hud.Dispose(), Times.Once);
    }

    [Fact]
    public void ReloadCreatesNewEntityAndDropsPendingClick()
    {
        using var f = new Fixture();
        f.Menu.Open(f.Player.Object);
        f.Click("Preview1");
        f.Catalog = f.Catalog.Cast<KnifeDefinition>().Select(knife => (IKnife)(knife with { Speed = 350 })).ToArray();
        f.Click("Equip1");
        Assert.Equal(2, f.Huds.Count);
        f.Click("Equip1", f.Entities[0]);
        f.Huds[0].Verify(hud => hud.Dispose(), Times.Once);
        f.Knives.Verify(service => service.SelectKnife(It.IsAny<IPlayer>(), It.IsAny<IKnife>()), Times.Never);
    }

    [Fact]
    public void AnotherHudCannotShareMouseCaptureWithKnifeMenu()
    {
        using var f = new Fixture();
        var other = new Mock<CCSCustomHudLayout>();
        other.SetupGet(value => value.IsValidEntity).Returns(true);
        other.Setup(value => value.IsInputCaptureEnabledForPlayer(3)).Returns(true);
        f.Layouts.Add(other.Object);
        Assert.False(f.Menu.Open(f.Player.Object));
        Assert.Empty(f.Huds);
        Assert.Null(f.Timer);
        f.Layouts.Clear();
        Assert.True(f.Menu.Open(f.Player.Object));
        f.Layouts.Add(other.Object);
        f.Update!();
        f.Huds[0].Verify(hud => hud.Dispose(), Times.Once);
        Assert.True(f.Timer!.IsCancellationRequested);
    }

    [Fact]
    public void DeadHumanCanStillChoosePreferenceBeforeRespawn()
    {
        using var f = new Fixture { Alive = false };
        Assert.True(f.Menu.Open(f.Player.Object));
        f.Click("Preview1");
        f.Click("Equip1");
        f.Knives.Verify(service => service.SelectKnife(f.Player.Object, f.Catalog[1]), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ZombieCanSaveSelectionAndReopenItBeforeBecomingHuman(bool alive)
    {
        using var f = new Fixture { Infected = true, Team = Team.T, Alive = alive };
        Assert.True(f.Menu.Open(f.Player.Object));
        f.Click("Preview1");
        f.Huds[^1].Verify(hud => hud.Text("EquipLabel", "SAVE"), Times.AtLeastOnce);
        f.Click("Equip1");
        f.Knives.Verify(service => service.SelectKnife(f.Player.Object, f.Catalog[1]), Times.Once);
        f.Huds[^1].Verify(hud => hud.Text("EquipLabel", "SAVED"), Times.AtLeastOnce);
        f.Huds[^1].Verify(hud => hud.Text("FooterStatus", It.Is<string>(value => value.Contains("Applies when you become human"))), Times.AtLeastOnce);
        f.Click("Close");
        Assert.True(f.Menu.Open(f.Player.Object));
        f.Huds[^1].Verify(hud => hud.Class("Row1", "Selected", true), Times.AtLeastOnce);
        f.Infected = false;
        f.Team = Team.CT;
        f.Click("Equip0");
        f.Huds[^1].Verify(hud => hud.Dispose(), Times.Once);
        f.Knives.Verify(service => service.SelectKnife(It.IsAny<IPlayer>(), It.IsAny<IKnife>()), Times.Once);
    }

    [Fact]
    public void SettingsBlockEquipmentAndKeepScaleUntilDisconnect()
    {
        using var f = new Fixture();
        f.Menu.Open(f.Player.Object);
        f.Click("Preview1");
        f.Click("Settings");
        f.Click("Equip1");
        f.Knives.Verify(service => service.SelectKnife(It.IsAny<IPlayer>(), It.IsAny<IKnife>()), Times.Never);
        f.Click("Scale85");
        f.Click("Close");
        f.Menu.Open(f.Player.Object);
        f.Huds[^1].Verify(hud => hud.Choice("KnifeRoot", "scale", "Scale85"), Times.AtLeastOnce);
        f.Core.Raise(core => core.Event.OnClientDisconnected += null, Mock.Of<IOnClientDisconnectedEvent>(ev => ev.PlayerId == 3));
        f.SessionId++;
        f.Menu.Open(f.Player.Object);
        f.Huds[^1].Verify(hud => hud.Choice("KnifeRoot", "scale", "Scale100"), Times.AtLeastOnce);
    }

    [Fact]
    public void ZombieStillNeedsPermissionAndSpectatorsCannotOpenMenu()
    {
        using var f = new Fixture { Infected = true, Team = Team.T, Allowed = false };
        Assert.True(f.Menu.Open(f.Player.Object));
        f.Click("Preview1");
        f.Click("Equip1");
        f.Knives.Verify(service => service.SelectKnife(It.IsAny<IPlayer>(), It.IsAny<IKnife>()), Times.Never);
        f.Click("Close");
        f.Team = Team.Spectator;
        Assert.False(f.Menu.Open(f.Player.Object));
    }

    [Fact]
    public void MissingResourceDoesNotStartTimerOrLeaveSessionOpen()
    {
        using var f = new Fixture { CreateFailure = true };
        Assert.False(f.Menu.Open(f.Player.Object));
        Assert.Null(f.Timer);
        f.CreateFailure = false;
        Assert.True(f.Menu.Open(f.Player.Object));
        Assert.Single(f.Huds);
    }

    private sealed class Fixture : IDisposable
    {
        public Mock<ISwiftlyCore> Core { get; } = new() { DefaultValue = DefaultValue.Mock };
        public Mock<IPlayer> Player { get; } = new();
        public Mock<IKnifeService> Knives { get; } = new();
        public List<Mock<IKnifeHudRuntime>> Huds { get; } = [];
        public List<CCSCustomHudLayout> Entities { get; } = [];
        public List<CCSCustomHudLayout> Layouts { get; } = [];
        public IKnife[] Catalog { get; set; } = [KnifeDefaults.Fallback, KnifeDefaults.Fallback with { InternalName = "other" }];
        public KnifeMenu Menu { get; }
        public ulong SessionId { get; set; } = 22;
        public bool Allowed { get; set; } = true;
        public bool Owns { get; set; } = true;
        public bool Alive { get; set; } = true;
        public Team Team { get; set; } = Team.CT;
        public bool Infected { get; set; }
        public bool CreateFailure { get; set; }
        private IKnife? _selected;
        public CancellationTokenSource? Timer { get; private set; }
        public Action? Update { get; private set; }

        public Fixture()
        {
            var controller = new Mock<CCSPlayerController>();
            controller.SetupGet(value => value.Team).Returns(() => Team);
            Player.SetupGet(value => value.Controller).Returns(controller.Object);
            Player.SetupGet(value => value.IsValid).Returns(true);
            Player.SetupGet(value => value.IsAlive).Returns(() => Alive);
            Player.SetupGet(value => value.PlayerID).Returns(3);
            Player.SetupGet(value => value.SteamID).Returns(123UL);
            Player.SetupGet(value => value.SessionId).Returns(() => SessionId);
            Core.Setup(value => value.EntitySystem.GetAllEntitiesByDesignerName<CCSCustomHudLayout>("custom_hud_layout")).Returns(() => Layouts);
            Core.Setup(value => value.PlayerManager.GetPlayer(3)).Returns(Player.Object);
            Core.Setup(value => value.MenusAPI.GetCurrentMenu(Player.Object)).Returns(() => null);
            Core.Setup(value => value.Scheduler.RepeatBySeconds(It.IsAny<float>(), It.IsAny<Action>()))
                .Returns((float _, Action update) => { Update = update; return Timer = new(); });
            var registry = new Mock<IKnivesRegistry>();
            registry.Setup(value => value.GetAll()).Returns(() => Catalog);
            Knives.Setup(value => value.GetKnife(Player.Object)).Returns(() => _selected ?? Catalog[0]);
            Knives.Setup(value => value.SelectKnife(Player.Object, It.IsAny<IKnife>()))
                .Callback<IPlayer, IKnife>((_, knife) => _selected = knife);
            var authorization = new Mock<IKnifeAuthorizationService>();
            authorization.Setup(value => value.CanUse(Player.Object, It.IsAny<IKnife>())).Returns(() => Allowed);
            var zombies = new Mock<IZombiePlagueApi> { DefaultValue = DefaultValue.Mock };
            zombies.Setup(value => value.IsInfected(Player.Object)).Returns(() => Infected);
            var localization = new Mock<ILocalizationApi>();
            Menu = new(Core.Object, registry.Object, Knives.Object, authorization.Object, zombies.Object,
                Options.Create(new KnifeHudOptions()), new(localization.Object), _ =>
                {
                    if (CreateFailure) throw new FileNotFoundException("VPK");
                    var entity = new Mock<CCSCustomHudLayout>().Object;
                    Entities.Add(entity);
                    var hud = new Mock<IKnifeHudRuntime>();
                    hud.SetupGet(value => value.IsValid).Returns(true);
                    hud.Setup(value => value.IsClassRegistered(It.IsAny<string>())).Returns(false);
                    hud.Setup(value => value.Owns(It.IsAny<CCSCustomHudLayout>())).Returns((CCSCustomHudLayout clicked) => Owns && ReferenceEquals(clicked, entity));
                    Huds.Add(hud);
                    return hud.Object;
                }, NullLogger<KnifeMenu>.Instance);
            Menu.RegisterCommands();
        }

        public void Click(string button, CCSCustomHudLayout? entity = null)
        {
            var ev = new Mock<IOnCustomHudClickedEvent>();
            ev.SetupGet(value => value.PlayerId).Returns(3);
            ev.SetupGet(value => value.ButtonId).Returns(button);
            ev.SetupGet(value => value.CustomHudLayout).Returns(entity ?? Entities[^1]);
            Core.Raise(core => core.Event.OnCustomHudClicked += null, ev.Object);
        }

        public void Dispose() { Menu.Dispose(); Timer?.Dispose(); }
    }
}
