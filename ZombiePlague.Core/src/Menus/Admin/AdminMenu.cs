using Admin.Api;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Permissions;
using ZombiePlague.Core.Menus.Admin.Round;

namespace ZombiePlague.Core.Menus.Admin;

internal sealed class AdminMenu(
    ISwiftlyCore core,
    IAdminApi adminApi,
    InfectMenu infectMenu,
    DisinfectMenu disinfectMenu,
    RoundMenu roundMenu
) : MenuBase(core)
{
    public override string Id => "zombie_plague.admin";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);

        if (adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Infect))
        {
            builder.AddOption(
                BuildInfectOption()
            );
        }

        if (adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Disinfect))
        {
            builder.AddOption(
                BuildDisinfectOption()
            );
        }
        
        if (adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Round))
        {
            builder.AddOption(BuildRoundOption());
        }

        return builder.Build();
    }
    
    protected override bool CanOpenCore(IPlayer player)
    {
        return HasAnyPermission(player);
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle("Админка Zombie Mode")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }
    
    private ButtonMenuOption BuildInfectOption()
    {
        var option = new ButtonMenuOption("Заразить игрока");

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTick(
                () => infectMenu.Open(args.Player)
            );

            return ValueTask.CompletedTask;
        };

        return option;
    }
    
    private ButtonMenuOption BuildDisinfectOption()
    {
        var option = new ButtonMenuOption("Вылечить игрока");

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTick(
                () => disinfectMenu.Open(args.Player)
            );

            return ValueTask.CompletedTask;
        };

        return option;
    }
    
    private ButtonMenuOption BuildRoundOption()
    {
        var option = new ButtonMenuOption("Управление раундами");

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTick(() => roundMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        return option;
    }
    
    private bool HasAnyPermission(IPlayer player)
    {
        return adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Infect) ||
               adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Disinfect) ||
               adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Round);
    }
}