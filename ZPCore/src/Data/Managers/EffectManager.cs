using ZPCore.Data.Effects.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZPApi.Data;
using ZPApi.Events;

namespace ZPCore.Data.Managers;

internal class EffectManager(ISwiftlyCore core, IEffectFactory effectFactory, IEventSubscriber eventSubscriber)
{
    
    private readonly List<IEffect> _effects = [];
    
    public void RegisterHooks()
    {
        core.GameEvent.HookPost<EventRoundStart>(EventRoundStart);
        
        eventSubscriber.OnEffectDestroyed += OnEffectDestroyed;
    }

    private void OnEffectDestroyed(IEffect effect)
    {
        _effects.Remove(effect);
    }
    
    private HookResult EventRoundStart(EventRoundStart @event)
    {
        foreach (var effect in _effects)
        {
            DestroyEffect(effect);
        }
        
        return HookResult.Continue;
    }
    
    public IEffect ApplyEffect<T>(IPlayer? caster, IPlayer target) where T : IEffect
    {
        return effectFactory.Create<T>(caster, target);
    }
    
    public void DestroyEffectByPlayer<T>(IPlayer target) where T : IEffect
    {
        var effect = _effects.Find(ef => ef is T && ef.Target.Equals(target));
        
        if (effect == null)
        {
            return;
        }
        
        DestroyEffect(effect);
    }
    
    public bool PlayerHasEffect<T>(IPlayer player) where T : IEffect
    {
        return _effects.Find(ef => ef is T && ef.Target.Equals(player)) != null;
    }

    private void DestroyEffect(IEffect effect)
    {
        effect.Destroy();
        _effects.Remove(effect);
    }

    public void AddEffect(IEffect effect)
    {
        _effects.Add(effect);
    }
}