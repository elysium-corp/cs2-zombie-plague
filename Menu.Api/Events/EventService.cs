using Menu.Api.Data.Contracts;

namespace Menu.Api.Events;

public sealed class EventService : IEventSubscriber, IEventPublisher
{
    public event EventDelegates.OnMenuAddOption? OnMenuAddOption;

    void IEventPublisher.OnMenuAddOption(Type menuType, DynamicOptionsMenu.MenuOptionsHolder holder)
    {
        OnMenuAddOption?.Invoke(menuType, holder);
    }
}