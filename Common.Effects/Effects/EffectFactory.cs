using Common.Effects.Effects.Contracts;
using Common.Effects.Effects.Settings;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Common.Effects.Effects;

internal sealed class EffectFactory(ISwiftlyCore core) : IEffectFactory
{
    public TEffect? Create<TEffect>(Action<IEffect> callback, IPlayer? caster, IPlayer target,
        IEffectSettings? settings = null) where TEffect : class, IEffect
    {
        return typeof(TEffect) switch
        {
            var t when t == typeof(Burn) =>
                new Burn(core, callback, caster, target, settings as BurnSettings) as TEffect,
            var t when t == typeof(Freeze) =>
                new Freeze(core, callback, caster, target, settings as FreezeSettings) as TEffect,
            var t when t == typeof(Disorient) => new Disorient(core, callback, caster, target,
                settings as DisorientSettings) as TEffect,
            var t when t == typeof(Vanish) => new Vanish(core, callback, caster, target,
                settings as VanishSettings) as TEffect,
            _ => throw new NotSupportedException("EffectFactory: type T hasn't supported!")
        };
    }
}