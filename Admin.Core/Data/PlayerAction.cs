using Admin.Api.Permissions;

namespace Admin.Core.Data;

internal enum PlayerAction { Money, Noclip, Grab, Release, Mute, Unmute, Gag, Ungag }

internal static class PlayerActionPermissions
{
    public static string For(PlayerAction action) => action switch
    {
        PlayerAction.Money => AdminPermissions.Money,
        PlayerAction.Noclip => AdminPermissions.Noclip,
        PlayerAction.Grab or PlayerAction.Release => AdminPermissions.Grab,
        PlayerAction.Mute or PlayerAction.Unmute => AdminPermissions.Mute,
        PlayerAction.Gag or PlayerAction.Ungag => AdminPermissions.Gag,
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };
}
