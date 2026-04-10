using SwiftlyS2.Shared.Players;

namespace Common.Effects.Effects.Contracts;

public interface IEffectFactory
{
    public IEffect Create<T>(IPlayer? caster, IPlayer target) where T : IEffect;
}