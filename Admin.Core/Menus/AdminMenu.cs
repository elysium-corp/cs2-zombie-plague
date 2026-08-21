using Admin.Api.Menus;
using Admin.Api.Permissions;
using Admin.Core.Services;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using Menu.Api.Extensions;
using SwiftlyS2.Core.Menus.OptionsBase;
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
    IPrivilegeService privilegeService,
    KickMenu kickMenu,
    BanMenu banMenu,
    KillMenu killMenu,
    RespawnMenu respawnMenu,
    RoundMenu roundMenu
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
        if (privilegeService.HasPermission(player.SteamID, AdminPermissions.Kick))
        {
            options.Add(BuildKickOption(), 1);
        }
        
        if (privilegeService.HasPermission(player.SteamID, AdminPermissions.Ban))
        {
            options.Add(BuildBanOption(), 2);
        }
        
        if (privilegeService.HasPermission(player.SteamID, AdminPermissions.Kill))
        {
            options.Add(BuildKillOption(), 3);
        }
        
        if (privilegeService.HasPermission(player.SteamID, AdminPermissions.Respawn))
        {
            options.Add(BuildRespawnOption(), 4);
        }
        
        if (privilegeService.HasPermission(player.SteamID, AdminPermissions.Round))
        {
            options.Add(BuildRoundOption(), 5);
        }
    }
    
    private ButtonMenuOption BuildKickOption()
    {
        var option = new ButtonMenuOption("Кикнуть");

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => kickMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        return option;
    }
    
    private ButtonMenuOption BuildBanOption()
    {
        var option = new ButtonMenuOption("Забанить");

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => banMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        return option;
    }
    
    private ButtonMenuOption BuildKillOption()
    {
        var option = new ButtonMenuOption("Убить");

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => killMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        return option;
    }
    
    private ButtonMenuOption BuildRespawnOption()
    {
        var option = new ButtonMenuOption("Возродить");

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => respawnMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        return option;
    }
    
    private ButtonMenuOption BuildRoundOption()
    {
        var option = new ButtonMenuOption("Управление раундом");

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => roundMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        return option;
    }
}