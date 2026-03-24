using SwiftlyS2.Shared.Players;
using ZPApi.Data;

namespace ZPApi.Events;

public sealed class EventService : IEventSubscriber, IEventPublisher
{
    public event EventDelegates.OnPlayerInfectedBy? OnPlayerInfectedBy;
    public event EventDelegates.OnPlayerInfected? OnPlayerInfected;
    public event EventDelegates.OnEffectDestroyed? OnEffectDestroyed;
    public event EventDelegates.OnGameRoundStarted? OnGameRoundStarted;

    void IEventPublisher.OnPlayerInfectedBy(IPlayer infector, IPlayer victim)
    {
        var handlers = OnPlayerInfectedBy;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnPlayerInfectedBy)@delegate;
            handler(infector, victim);

        }
    }
    
    void IEventPublisher.OnPlayerInfected(IPlayer victim)
    {
        var handlers = OnPlayerInfected;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnPlayerInfected)@delegate;
            handler(victim);
        }
    }
    
    void IEventPublisher.OnEffectDestroyed(IEffect effect)
    {
        var handlers = OnEffectDestroyed;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnEffectDestroyed)@delegate;
            handler(effect);
        }
    }
    
    void IEventPublisher.OnGameRoundStarted(IRound round)
    {
        var handlers = OnGameRoundStarted;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnGameRoundStarted)@delegate;
            handler(round);
        }
    }
}