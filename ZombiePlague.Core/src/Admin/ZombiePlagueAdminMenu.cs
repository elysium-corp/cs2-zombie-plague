using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Admin;

internal sealed class ZombiePlagueAdminMenu(ISwiftlyCore core) : MenuBase(core)
{
    public override string Id => "zombie_plague.admin";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override IMenuAPI Build(IPlayer player)
    {
        return CreateBuilder(player)
            .AddOption(
                new TextMenuOption
                {
                    Text = "Раздел подключён"
                }
            )
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