using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Menus.Admin;

internal sealed class AdminMenu(
    ISwiftlyCore core, 
    InfectMenu infectMenu) : MenuBase(core)
{
    public override string Id => "zombie_plague.admin";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);

        var infectOption = new ButtonMenuOption("Сделать зомби");

        infectOption.Click += (_, args) =>
        {
            Core.Scheduler.NextTick(
                () => infectMenu.Open(args.Player)
            );

            return ValueTask.CompletedTask;
        };

        return builder
            .AddOption(infectOption)
            .Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle("Админка Zombie Mode")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }
}