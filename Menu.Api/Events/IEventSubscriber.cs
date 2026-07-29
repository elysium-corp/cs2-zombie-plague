namespace Menu.Api.Events;

public interface IEventSubscriber
{
    event EventDelegates.OnMenuAddOption? OnMenuAddOption;
    
    event EventDelegates.OnMainMenuAddOption? OnMainMenuAddOption;
    
    event EventDelegates.OnZClassMenuAddOption? OnZClassMenuAddOption;
}