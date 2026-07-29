using Menu.Api.Data.Contracts;
using SwiftlyS2.Shared.Players;

namespace Menu.Api.Events;

public interface IEventPublisher
{
    void OnMenuAddOption(IPlayer player, Type menuType, DynamicOptionsMenu.MenuOptionsHolder holder);
    
    void OnMainMenuAddOption(IPlayer player, DynamicOptionsMenu.MenuOptionsHolder holder);
    
    void OnZClassMenuAddOption(IPlayer player, DynamicOptionsMenu.MenuOptionsHolder holder);
}