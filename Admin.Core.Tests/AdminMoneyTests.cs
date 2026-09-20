using Admin.Core.Services;
using Economy.Api;
using Moq;
using SwiftlyS2.Shared.Players;
using Xunit;

namespace Admin.Core.Tests;

public sealed class AdminMoneyTests
{
    [Fact]
    public void Grant_UsesEconomyApiAndReportsActualAmountAfterLimit()
    {
        var player = Player();
        var economy = new Mock<IEconomyApi>();
        economy.SetupSequence(api => api.GetBalance(player.Object)).Returns(15500).Returns(16000);
        var service = new AdminMoneyService { Economy = economy.Object };
        Assert.Equal(500, service.Give(player.Object, 1000));
        economy.Verify(api => api.GiveMoney(player.Object, 1000), Times.Once);
    }

    [Fact]
    public void RejectedTransaction_IsNotReportedAsCredit()
    {
        var player = Player();
        var economy = new Mock<IEconomyApi>();
        economy.Setup(api => api.GetBalance(player.Object)).Returns(1000);
        Assert.Equal(0, new AdminMoneyService { Economy = economy.Object }.Give(player.Object, 1000));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidAmount_DoesNotCallEconomy(int amount)
    {
        var economy = new Mock<IEconomyApi>(MockBehavior.Strict);
        Assert.Equal(0, new AdminMoneyService { Economy = economy.Object }.Give(Player().Object, amount));
    }

    [Fact]
    public void MissingEconomyOrDisconnectedPlayer_FailsWithoutMutation()
    {
        var player = Player();
        Assert.Equal(0, new AdminMoneyService().Give(player.Object, 1000));
        player.SetupGet(value => value.IsValid).Returns(false);
        var economy = new Mock<IEconomyApi>(MockBehavior.Strict);
        Assert.Equal(0, new AdminMoneyService { Economy = economy.Object }.Give(player.Object, 1000));
    }

    private static Mock<IPlayer> Player()
    {
        var player = new Mock<IPlayer>();
        player.SetupGet(value => value.IsValid).Returns(true);
        player.SetupGet(value => value.IsAuthorized).Returns(true);
        return player;
    }
}
