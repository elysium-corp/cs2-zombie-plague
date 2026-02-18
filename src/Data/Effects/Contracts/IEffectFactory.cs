using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Effects.Contracts;

public interface IEffectFactory
{
    public BaseEffect Create<T>(IPlayer? caster, IPlayer target) where T : BaseEffect;
}