using CS2ZombiePlague.Data.Effects.Contracts;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Managers;

public class EffectManager(ISwiftlyCore core)
{
    private readonly IEffectFactory _effectFactory = DependencyManager.GetService<IEffectFactory>();
    
    private readonly List<BaseEffect> _effects = [];
    
    public void RegisterHooks()
    {
        core.GameEvent.HookPost<EventRoundStart>(EventRoundStart);
    }
    
    private HookResult EventRoundStart(EventRoundStart @event)
    {
        foreach (var effect in _effects)
        {
            DestroyEffect(effect);
        }
        
        return HookResult.Continue;
    }
    
    public BaseEffect ApplyEffect<T>(IPlayer? caster, IPlayer target) where T : BaseEffect
    {
        return _effectFactory.Create<T>(caster, target);
    }
    
    public void DestroyEffectByPlayer<T>(IPlayer target) where T : BaseEffect
    {
        var effect = _effects.Find(ef => ef is T && ef.Target.Equals(target));
        
        if (effect == null)
        {
            return;
        }
        
        DestroyEffect(effect);
    }
    
    public bool PlayerHasEffect<T>(IPlayer player) where T : BaseEffect
    {
        return _effects.Find(ef => ef is T && ef.Target.Equals(player)) != null;
    }

    private void DestroyEffect(BaseEffect effect)
    {
        effect.DestroyEffect();
        _effects.Remove(effect);
    }

    public void AddEffect(BaseEffect effect)
    {
        _effects.Add(effect);
    }
}