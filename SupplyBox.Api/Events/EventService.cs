using SupplyBox.Data;
using SwiftlyS2.Shared.Players;

namespace SupplyBox.Events;

public sealed class EventService : IEventSubscriber, IEventPublisher
{
    public event EventDelegates.OnSupplyBoxDropped? OnSupplyBoxDropped;
    public event EventDelegates.OnSupplyBoxPickedUp? OnSupplyBoxPickedUp;

    void IEventPublisher.OnSupplyBoxDropped(ISupplyBoxEntity supplyBox)
    {
        var handlers = OnSupplyBoxDropped;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnSupplyBoxDropped)@delegate;
            handler(supplyBox);
        }
    }
    
    void IEventPublisher.OnSupplyBoxPickedUp(IPlayer player, ISupplyBoxEntity supplyBox)
    {
        var handlers = OnSupplyBoxPickedUp;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnSupplyBoxPickedUp)@delegate;
            handler(player, supplyBox);
        }
    }
}