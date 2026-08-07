using Menu.Api.Data.Contracts;
using Menu.Api.Data.Menus;
using Menu.Api.Events;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Menu.Core.Menus;

public class ZClassMenu(ISwiftlyCore core, IEventPublisher eventPublisher) : DynamicOptionsMenu(core, eventPublisher), IZClassMenu
{
    protected override Action<IPlayer, MenuOptionsHolder> MenuBuilderCallback => eventPublisher.OnZClassMenuAddOption;
    
    public override IMenuBuilderAPI Design(IPlayer player, IMenuDesignAPI design)
    {
        var locale = Core.Translation.GetPlayerLocalizer(player);

        return design
            .SetMenuTitle(locale["Menu.ZClass.Title"])
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems()
            .Design.SetMenuFooterVisible()
            .Design.SetMenuTitleItemCountVisible()
            .Design.SetMaxVisibleItems();
    }
}