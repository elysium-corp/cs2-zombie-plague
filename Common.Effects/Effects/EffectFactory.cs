using Common.Effects.Effects.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Common.Effects.Effects;

internal sealed class EffectFactory(ISwiftlyCore core) : IEffectFactory
{
    public IEffect Create<T>(Action<IEffect> callback, IPlayer? caster, IPlayer target) where T : IEffect
    {
        return typeof(T) switch
        {
            var t when t == typeof(Burn) => new Burn(core, callback, caster, target),
            var t when t == typeof(Freeze) => new Freeze(core, callback, caster, target),
            var t when t == typeof(Disorient) => new Disorient(core, callback, caster, target),
            _ => throw new NotSupportedException("EffectFactory: type T hasn't supported!")
        };
    }
}