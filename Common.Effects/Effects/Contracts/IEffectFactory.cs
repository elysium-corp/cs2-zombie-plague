using SwiftlyS2.Shared.Players;

namespace Common.Effects.Effects.Contracts;

internal interface IEffectFactory
{
    public TEffect? Create<TEffect>(Action<IEffect> callback, IPlayer? caster, IPlayer target,
        IEffectSettings? settings = null) where TEffect : class, IEffect;
}