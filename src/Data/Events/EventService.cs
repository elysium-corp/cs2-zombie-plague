using CS2ZombiePlague.Data.ZClasses;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Events;

public sealed class EventService : IEventSubscriber, IEventPublisher
{
    public event EventDelegates.OnPlayerInfected? OnPlayerInfected;
    
    void IEventPublisher.OnPlayerInfected(IPlayer player, IZClass zClass)
    {
        var handlers = OnPlayerInfected;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnPlayerInfected)@delegate;
            try { handler(player, zClass); }
            catch (Exception ex)
            {
                // add custom logger
            }
        }
    }
}