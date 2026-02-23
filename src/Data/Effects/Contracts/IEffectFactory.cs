using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Effects.Contracts;

public interface IEffectFactory
{
    public IEffect Create<T>(IPlayer? caster, IPlayer target) where T : IEffect;
}