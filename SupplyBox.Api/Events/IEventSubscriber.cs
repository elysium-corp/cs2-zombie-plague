namespace SupplyBox.Events;

public interface IEventSubscriber
{
    event EventDelegates.OnSupplyBoxDropped? OnSupplyBoxDropped;
    event EventDelegates.OnSupplyBoxPickedUp? OnSupplyBoxPickedUp;
}