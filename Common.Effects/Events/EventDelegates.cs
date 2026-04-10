using Common.Effects.Effects.Contracts;

namespace Common.Effects.Events;

public sealed class EventDelegates
{
    public delegate void OnEffectDestroyed(IEffect effect);  
    public delegate void OnEffectCreated(IEffect effect);  
}