using SwiftlyS2.Shared.Players;

namespace Common.Effects.Effects.Contracts;

internal interface IEffectFactory
{
    public IEffect Create<T>(Action<IEffect> callback, IPlayer? caster, IPlayer target) where T : IEffect;
}