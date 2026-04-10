using Common.Effects.Effects.Contracts;
using Common.Effects.Events;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Common.Effects.Effects;

internal sealed class EffectFactory(ISwiftlyCore core, IEventPublisher eventPublisher) : IEffectFactory
{
    public IEffect Create<T>(IPlayer? caster, IPlayer target) where T : IEffect
    {
        return typeof(T) switch
        {
            var t when t == typeof(Burn) => new Burn(core, eventPublisher, caster, target),
            var t when t == typeof(Freeze) => new Freeze(core, eventPublisher, caster, target),
            _ => throw new NotSupportedException("EffectFactory: type T hasn't supported!")
        };
    }
}