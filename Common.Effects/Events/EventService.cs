using Common.Effects.Effects.Contracts;

namespace Common.Effects.Events;

public sealed class EventService : IEventSubscriber, IEventPublisher
{
    public event EventDelegates.OnEffectDestroyed? OnEffectDestroyed;
    public event EventDelegates.OnEffectCreated? OnEffectCreated;
    
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
    
    void IEventPublisher.OnEffectCreated(IEffect effect)
    {
        var handlers = OnEffectCreated;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnEffectCreated)@delegate;
            handler(effect);
        }
    }
}