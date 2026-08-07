using Menu.Api.Data.Contracts;
using SwiftlyS2.Shared.Players;

namespace Menu.Api.Events;

public sealed class EventService : IEventSubscriber, IEventPublisher
{
    public event EventDelegates.OnMenuAddOption? OnMenuAddOption;
    public event EventDelegates.OnMainMenuAddOption? OnMainMenuAddOption;
    public event EventDelegates.OnZClassMenuAddOption? OnZClassMenuAddOption;

    void IEventPublisher.OnMenuAddOption(IPlayer player, Type menuType, DynamicOptionsMenu.MenuOptionsHolder holder)
    {
        OnMenuAddOption?.Invoke(player, menuType, holder);
    }

    void IEventPublisher.OnMainMenuAddOption(IPlayer player, DynamicOptionsMenu.MenuOptionsHolder holder)
    {
        OnMainMenuAddOption?.Invoke(player, holder);
    }
    
    void IEventPublisher.OnZClassMenuAddOption(IPlayer player, DynamicOptionsMenu.MenuOptionsHolder holder)
    {
        OnZClassMenuAddOption?.Invoke(player, holder);
    }
}