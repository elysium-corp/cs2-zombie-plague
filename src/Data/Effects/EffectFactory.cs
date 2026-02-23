using CS2ZombiePlague.Data.Effects.Contracts;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Effects;

public class EffectFactory : IEffectFactory
{
    public IEffect Create<T>(IPlayer? caster, IPlayer target) where T : IEffect
    {
        return typeof(T) switch
        {
            var t when t == typeof(Burn) => new Burn(caster, target),
            var t when t == typeof(Freeze) => new Freeze(caster, target),
            _ => throw new NotSupportedException("EffectFactory: type T hasn't supported!")
        };
    }
}