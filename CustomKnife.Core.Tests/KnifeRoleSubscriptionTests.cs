using Common.Hooks;
using CustomKnife.Data.Menus;
using CustomKnife.Data.Registrator;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Hud;
using CustomKnife.Services;
using Localization.Api;
using Menu.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using Xunit;
using ZombiePlague.Api;
using ZombiePlague.Api.Events.Contexts.Player;

namespace CustomKnife.Core.Tests;

public sealed class KnifeRoleSubscriptionTests
{
    [Fact]
    public void HumanTransitionsRequestSavedKnifeAndStopRemovesSubscriptions()
    {
        var hooks = new HookService((error, _, _) => throw error);
        var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
        var zombies = new Mock<IZombiePlagueApi> { DefaultValue = DefaultValue.Mock };
        zombies.Setup(value => value.Events.Players.Humanized).Returns(new HookEvent<PlayerHumanizedContext>(hooks));
        zombies.Setup(value => value.Events.Players.Disinfected).Returns(new HookEvent<PlayerDisinfectedContext>(hooks));
        zombies.Setup(value => value.Events.Players.RoleApplied).Returns(new HookEvent<PlayerRoleAppliedContext>(hooks));
        zombies.Setup(value => value.Events.Players.BecameSurvivor).Returns(new HookEvent<PlayerBecameSurvivorContext>(hooks));
        var knives = new Mock<IKnifeService>();
        var localization = Mock.Of<ILocalizationApi>();
        using var menu = new KnifeMenu(core.Object, Mock.Of<IKnivesRegistry>(), knives.Object,
            Mock.Of<IKnifeAuthorizationService>(), zombies.Object, Options.Create(new KnifeHudOptions()),
            new(localization), _ => throw new InvalidOperationException(), NullLogger<KnifeMenu>.Instance);
        var bridge = new MenuApiBridge();
        bridge.Initialize(new Mock<IMenuApi> { DefaultValue = DefaultValue.Mock }.Object);
        var coordinator = new CustomKnifeCoordinator(core.Object, knives.Object, Mock.Of<IPlayerKnifeService>(),
            menu, bridge, localization, zombies.Object);
        var player = Mock.Of<IPlayer>();
        var humanized = new PlayerHumanizedContext(player);
        var disinfected = new PlayerDisinfectedContext(player);
        var applied = new PlayerRoleAppliedContext(player);
        var survivor = new PlayerBecameSurvivorContext(player);

        coordinator.Start();
        hooks.Dispatch(ref humanized);
        hooks.Dispatch(ref disinfected);
        hooks.Dispatch(ref applied);
        hooks.Dispatch(ref survivor);
        knives.Verify(value => value.TryGiveKnife(player), Times.Exactly(4));
        coordinator.Stop();
        hooks.Dispatch(ref humanized);
        hooks.Dispatch(ref disinfected);
        hooks.Dispatch(ref applied);
        hooks.Dispatch(ref survivor);
        knives.Verify(value => value.TryGiveKnife(player), Times.Exactly(4));
    }
}
