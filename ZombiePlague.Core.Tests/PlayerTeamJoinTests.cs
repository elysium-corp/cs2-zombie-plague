using Common.Hooks.Abstractions;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Core.Data.Controllers;
using ZombiePlague.Core.Data.Entities.Human;
using ZombiePlague.Core.Data.Entities.Human.Factory;
using ZombiePlague.Core.Data.Managers;

namespace ZombiePlague.Core.Tests;

public sealed class PlayerTeamJoinTests
{
    [Theory]
    [InlineData(Team.None, true)]
    [InlineData(Team.Spectator, true)]
    [InlineData(Team.T, false)]
    [InlineData(Team.CT, false)]
    public void HumanizingPlayersOutsideTheTeamsJoinsInsteadOfSwitchingSides(Team current, bool joins)
    {
        var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
        var controller = new Mock<CCSPlayerController>();
        controller.SetupGet(value => value.IsValid).Returns(true);
        controller.SetupGet(value => value.Team).Returns(current);
        var player = new Mock<IPlayer>();
        player.SetupGet(value => value.IsValid).Returns(true);
        player.SetupGet(value => value.Controller).Returns(controller.Object);
        var repository = Mock.Of<IPlayerRepository>(value => value.GetHClassId(It.IsAny<IPlayer>()) == "default");
        var manager = new PlayerManager(
            new HumanController(core.Object, repository, new HumanClasses()),
            new ZombieController(core.Object, repository, null!),
            null!,
            Mock.Of<IHookPublisher>());

        Assert.True(manager.TrySetHuman(player.Object));

        // Без штатного входа в команду игрок числится в CT, но Respawn не может его возродить.
        player.Verify(value => value.ChangeTeam(Team.CT), joins ? Times.Once() : Times.Never());
        player.Verify(value => value.SwitchTeam(Team.CT), joins ? Times.Never() : Times.Once());
    }

    private sealed class HumanClasses : IHClassFactory
    {
        public IHClass Create<TClass>(ulong steamId = 0) where TClass : IHClass => Mock.Of<IHClass>();
        public IHClass CreateOrDefault(string classId, ulong steamId = 0) => Mock.Of<IHClass>();
    }
}
