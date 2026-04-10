namespace Common.Effects.Events;

public interface IEventSubscriber
{
    event EventDelegates.OnEffectCreated? OnEffectCreated;
    event EventDelegates.OnEffectDestroyed? OnEffectDestroyed;
}