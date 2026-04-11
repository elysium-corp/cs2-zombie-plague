using Common.Effects.Effects.Contracts;
using SwiftlyS2.Shared.Players;

namespace Common.Effects.Contracts;

public interface IEffectService
{
    public IEffect ApplyEffect<TEffect>(IPlayer? caster, IPlayer target) where TEffect : IEffect;
    
    public void DestroyEffect<TEffect>(IPlayer target) where TEffect : IEffect;

    public bool HasEffect<TEffect>(IPlayer player) where TEffect : IEffect;

    public void DestroyAllEffects();
}