using Common.Effects.Effects.Contracts;

namespace Common.Effects.Events;

public interface IEventPublisher
{
    void OnEffectDestroyed(IEffect effect);
    void OnEffectCreated(IEffect effect);
}