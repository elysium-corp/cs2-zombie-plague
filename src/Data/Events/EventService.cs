using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Events;

public sealed class EventService : IEventSubscriber, IEventPublisher
{
    public event EventDelegates.OnPlayerInfectedBy? OnPlayerInfectedBy;

    void IEventPublisher.OnPlayerInfectedBy(IPlayer infector, IPlayer victim)
    {
        var handlers = OnPlayerInfectedBy;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnPlayerInfectedBy)@delegate;
            try { handler(infector, victim); }
            catch (Exception ex)
            {
                // add custom logger
            }
        }
    }
}