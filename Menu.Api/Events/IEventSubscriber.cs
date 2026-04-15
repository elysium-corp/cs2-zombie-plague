namespace Menu.Api.Events;

public interface IEventSubscriber
{
    event EventDelegates.OnMenuAddOption? OnMenuAddOption;
}