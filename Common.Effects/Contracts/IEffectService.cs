using Common.Effects.Effects.Contracts;
using SwiftlyS2.Shared.Players;

namespace Common.Effects.Contracts;

public interface IEffectService
{
    public void Initialize();
    
    public void Dispose();
    
    public IEffect ApplyEffect<T>(IPlayer? caster, IPlayer target) where T : IEffect;
    
    public void DestroyEffectByPlayer<T>(IPlayer target) where T : IEffect;

    public bool PlayerHasEffect<T>(IPlayer player) where T : IEffect;
}