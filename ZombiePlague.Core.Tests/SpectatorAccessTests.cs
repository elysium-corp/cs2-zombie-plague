using Admin.Api;
using Admin.Api.Permissions;
using Microsoft.Extensions.Options;
using Moq;
using SwiftlyS2.Shared.Players;
using Xunit;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Data.Service;

namespace ZombiePlague.Core.Tests;

public sealed class SpectatorAccessTests
{
    [Fact]
    public void AdminSpectateIsTheDefaultPermission()
    {
        Assert.Equal([AdminPermissions.Spectate], new ZombiePlagueCoreConfig().SpectatorPermissions);
    }

    [Theory]
    [InlineData(new[] { "admin.spectate" }, "admin.spectate", true)]
    [InlineData(new[] { "admin.kick", "admin.spectate" }, "admin.kick", true)]
    [InlineData(new[] { "admin.spectate" }, "admin.kick", false)]
    [InlineData(new string[0], "admin.spectate", false)]
    [InlineData(new[] { " " }, " ", false)]
    public void AnyConfiguredPermissionAllowsSpectating(string[] configured, string granted, bool expected)
    {
        var access = Access(configured, granted, out var player);

        Assert.Equal(expected, access.CanSpectate(player));
    }

    [Fact]
    public void BotsCannotSpectate()
    {
        var access = Access(["admin.spectate"], "admin.spectate", out _);
        var bot = Mock.Of<IPlayer>(value => value.SteamID == 0UL);

        Assert.False(access.CanSpectate(bot));
    }

    [Fact]
    public void OnlyTheOwnChoiceInTheCurrentConnectionAndMapKeepsAPlayerInSpectators()
    {
        var admin = new Mock<IAdminApi>();
        var session = 10UL;
        var player = new Mock<IPlayer>();
        player.SetupGet(value => value.SteamID).Returns(76561198000000001UL);
        player.SetupGet(value => value.PlayerID).Returns(3);
        player.SetupGet(value => value.SessionId).Returns(() => session);
        admin.Setup(value => value.HasPermission(player.Object, "admin.spectate")).Returns(true);
        var access = new SpectatorAccess(admin.Object, Options.Create(new ZombiePlagueCoreConfig()));

        // Право само по себе не держит игрока в наблюдателях: нужен его собственный выбор.
        Assert.False(access.IsVoluntarySpectator(player.Object));
        access.MarkVoluntarySpectator(player.Object);
        Assert.True(access.IsVoluntarySpectator(player.Object));

        // Переподключение — новое подключение в том же слоте: игрок входит в игру как все.
        session = 11;
        Assert.False(access.IsVoluntarySpectator(player.Object));

        access.MarkVoluntarySpectator(player.Object);
        access.ForgetAll();
        Assert.False(access.IsVoluntarySpectator(player.Object));

        access.MarkVoluntarySpectator(player.Object);
        access.Forget(player.Object);
        Assert.False(access.IsVoluntarySpectator(player.Object));

        access.MarkVoluntarySpectator(player.Object);
        admin.Setup(value => value.HasPermission(player.Object, "admin.spectate")).Returns(false);
        Assert.False(access.IsVoluntarySpectator(player.Object));
    }

    private static SpectatorAccess Access(string[] configured, string granted, out IPlayer player)
    {
        var admin = new Mock<IAdminApi>();
        var target = Mock.Of<IPlayer>(value => value.SteamID == 76561198000000001UL);
        admin.Setup(value => value.HasPermission(target, granted)).Returns(true);
        player = target;

        return new SpectatorAccess(admin.Object, Options.Create(new ZombiePlagueCoreConfig { SpectatorPermissions = configured }));
    }
}
