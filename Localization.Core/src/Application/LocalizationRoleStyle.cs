using Admin.Api;
using Admin.Api.Data;
using Localization.Api;
using SwiftlyS2.Shared.Players;

namespace Localization.Core.Application;

internal sealed class LocalizationRoleStyle
{
    private IAdminApi? _admin;
    internal void Bind(IAdminApi? admin) => _admin = admin;

    internal LocalizationPlayerStyle Resolve(IPlayer player) => Select(_admin?.GetPlayerPrivileges(player) ?? []);

    internal static LocalizationPlayerStyle Select(IEnumerable<IPrivilege> privileges)
    {
        var role = privileges.OrderByDescending(x => x.ColorPriority)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        return role is null ? new() : new(role.Key, role.DisplayName,
            LocalizationColorSchema.SupportedColors.Contains(role.ChatColor) ? role.ChatColor : "default",
            System.Text.RegularExpressions.Regex.IsMatch(role.HudColor, "^#[a-fA-F0-9]{6}$") ? role.HudColor : "#ffffff");
    }
}
