using Admin.Api.Permissions;
using Admin.Core.Data;
using Admin.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using Xunit;

namespace Admin.Core.Tests;

public sealed class PlayerActionAuthorizationTests
{
    [Theory]
    [InlineData((int)PlayerAction.Money, AdminPermissions.Money)]
    [InlineData((int)PlayerAction.Noclip, AdminPermissions.Noclip)]
    [InlineData((int)PlayerAction.Grab, AdminPermissions.Grab)]
    [InlineData((int)PlayerAction.Release, AdminPermissions.Grab)]
    [InlineData((int)PlayerAction.Mute, AdminPermissions.Mute)]
    [InlineData((int)PlayerAction.Unmute, AdminPermissions.Mute)]
    [InlineData((int)PlayerAction.Gag, AdminPermissions.Gag)]
    [InlineData((int)PlayerAction.Ungag, AdminPermissions.Gag)]
    public void EachAction_RequiresItsOwnPermission(int actionValue, string permission)
    {
        var action = (PlayerAction)actionValue;
        var player = new Mock<IPlayer>();
        player.SetupGet(value => value.IsValid).Returns(true);
        player.SetupGet(value => value.IsAuthorized).Returns(true);
        player.SetupGet(value => value.SteamID).Returns(42UL);
        var privileges = new TestPrivileges();
        var service = new AdminPlayerActionService(null!, privileges, null!, null!, null!, null!, NullLogger<AdminPlayerActionService>.Instance);
        service.Start();
        Assert.False(service.CanUse(player.Object, action));
        privileges.Permission = permission;
        Assert.True(service.CanUse(player.Object, action));
        service.Stop();
        Assert.False(service.CanUse(player.Object, action));
        Assert.False(service.CanUse(null, action));
    }

    [Fact]
    public void StaleMenuTarget_DoesNotResolveNewPlayerInSameSlot()
    {
        var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
        var replacement = new Mock<IPlayer>();
        replacement.SetupGet(player => player.IsValid).Returns(true);
        replacement.SetupGet(player => player.SessionId).Returns(102UL);
        core.Setup(value => value.PlayerManager.GetPlayer(3)).Returns(replacement.Object);
        Assert.Null(new PlayerActionTarget(3, 101).Resolve(core.Object));
        Assert.Same(replacement.Object, new PlayerActionTarget(3, 102).Resolve(core.Object));
    }
    private sealed class TestPrivileges : IPrivilegeService
    {
        public string? Permission { get; set; }
        public IReadOnlyCollection<global::Admin.Api.Data.IPrivilege> GetPrivileges(ulong steamId) => [];
        public bool HasPrivilege(ulong steamId, string key) => false;
        public bool HasPermission(ulong steamId, string permission) => steamId == 42 && Permission == permission;
    }
}
