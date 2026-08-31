using Admin.Api.Menus;
using Admin.Api.Permissions;
using Admin.Core.Services;
using Localization.Api;
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
    RoundMenu roundMenu,
    ILocalizationApi localization
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
            .SetMenuTitle(localization.GetForPlayerOrKey(player, "Admin.Menu.Title"))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    protected override void BuildOptions(IPlayer player, MenuOptionsCollection options)
    {
        if (privilegeService.HasPermission(player.SteamID, AdminPermissions.Kick))
        {
            options.Add(BuildKickOption(player), 1);
        }
        
        if (privilegeService.HasPermission(player.SteamID, AdminPermissions.Ban))
        {
            options.Add(BuildBanOption(player), 2);
        }
        
        if (privilegeService.HasPermission(player.SteamID, AdminPermissions.Kill))
        {
            options.Add(BuildKillOption(player), 3);
        }
        
        if (privilegeService.HasPermission(player.SteamID, AdminPermissions.Respawn))
        {
            options.Add(BuildRespawnOption(player), 4);
        }
        
        if (privilegeService.HasPermission(player.SteamID, AdminPermissions.Round))
        {
            options.Add(BuildRoundOption(player), 5);
        }
    }
    
    private ButtonMenuOption BuildKickOption(IPlayer player)
    {
        var option = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "Admin.Menu.Kick"));

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => kickMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        return option;
    }
    
    private ButtonMenuOption BuildBanOption(IPlayer player)
    {
        var option = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "Admin.Menu.Ban"));

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => banMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        return option;
    }
    
    private ButtonMenuOption BuildKillOption(IPlayer player)
    {
        var option = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "Admin.Menu.Kill"));

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => killMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        return option;
    }
    
    private ButtonMenuOption BuildRespawnOption(IPlayer player)
    {
        var option = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "Admin.Menu.Respawn"));

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => respawnMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        return option;
    }
    
    private ButtonMenuOption BuildRoundOption(IPlayer player)
    {
        var option = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "Admin.Menu.Round"));

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => roundMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        return option;
    }
}
