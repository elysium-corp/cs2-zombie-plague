using Menu.Api.Data.Contracts;
using Menu.Api.Events;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Menu.Core.Data.Menus;

internal class Main(ISwiftlyCore core, IEventPublisher eventPublisher) : DynamicOptionsMenu(core, eventPublisher)
{
    public HashSet<string> Commands => ["menu", "main", "меню", "ьутг", "vty."];
    
    public override IMenuBuilderAPI Design(IPlayer player, IMenuDesignAPI design)
    {
        var locale = core.Translation.GetPlayerLocalizer(player);
        
        return design
            .SetMenuTitle(locale["Menu.Main.Title"])
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }
}