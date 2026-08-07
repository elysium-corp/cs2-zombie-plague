using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data;

namespace ZombiePlague.Api.Events;

public sealed class EventService : IEventSubscriber, IEventPublisher
{
    public event EventDelegates.OnPlayerInfected? OnPlayerInfected;
    public event EventDelegates.OnPlayerDisinfected? OnPlayerDisinfected;
    
    // Round API
    public event EventDelegates.OnRoundStarted? OnRoundStarted;
    public event EventDelegates.OnRoundEnded? OnRoundEnded;
    
    void IEventPublisher.OnPlayerInfected(IPlayer infected, IPlayer? infector)
    {
        var handlers = OnPlayerInfected;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnPlayerInfected)@delegate;
            handler(infected, infector);
        }
    }

    void IEventPublisher.OnPlayerDisinfected(IPlayer disinfected)
    {
        var handlers = OnPlayerDisinfected;
        if (handlers == null) return;

        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnPlayerDisinfected)@delegate;
            handler(disinfected);
        }
    }
    
    void IEventPublisher.OnRoundStarted(IRound round)
    {
        var handlers = OnRoundStarted;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnRoundStarted)@delegate;
            handler(round);
        }
    }
    
    void IEventPublisher.OnRoundEnded(IRound round)
    {
        var handlers = OnRoundEnded;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnRoundEnded)@delegate;
            handler(round);
        }
    }
}