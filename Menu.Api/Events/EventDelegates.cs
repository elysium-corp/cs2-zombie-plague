using Menu.Api.Data.Contracts;
using SwiftlyS2.Shared.Players;

namespace Menu.Api.Events;

public static class EventDelegates
{
    public delegate void OnMenuAddOption(IPlayer player, Type menuType, DynamicOptionsMenu.MenuOptionsHolder holder);

    public delegate void OnMainMenuAddOption(IPlayer player, DynamicOptionsMenu.MenuOptionsHolder holder);
    
    public delegate void OnZClassMenuAddOption(IPlayer player, DynamicOptionsMenu.MenuOptionsHolder holder);
}