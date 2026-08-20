using Admin.Api.Menus;
using Admin.Api.Permissions;
using Admin.Core.Services;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using Menu.Api.Extensions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Menus;

/// <summary>
/// Главное административное меню.
///
/// Меню доступно только игрокам с разрешением <c>admin.menu</c>
/// и может динамически расширяться другими модулями.
/// </summary>
internal sealed class AdminMenu(
    ISwiftlyCore core,
    IMenuExtensionDispatcher extensionDispatcher,
    IPrivilegeService privilegeService
) : DynamicOptionsMenu(core, extensionDispatcher)
{
    public override string Id => AdminMenuIds.Main;

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override IReadOnlyCollection<string> Commands { get; } =
    [
        "admin",
        "adminmenu",
        "админ",
        "админка",
        "фвьшт",
        "фвьштьутг"
    ];

    protected override bool CanOpenCore(IPlayer player)
    {
        return privilegeService.HasPermission(player.SteamID, AdminPermissions.Menu);
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle("Админ меню")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    protected override void BuildOptions(IPlayer player, MenuOptionsCollection options)
    {
        // Базовых пунктов пока нет.
        //
        // Пункты добавляются другими модулями через
        // IMenuExtensionRegistry.Subscribe(AdminMenuIds.Main, ...).
    }
}