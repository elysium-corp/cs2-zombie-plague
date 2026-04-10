using Common.Effects.Contracts;
using Common.Effects.Effects.Contracts;
using Common.Effects.Events;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace Common.Effects;

public sealed class EffectService(ISwiftlyCore core, IEffectFactory effectFactory, IEventSubscriber eventSubscriber) : IEffectService
{
    private readonly List<IEffect> _allEffects = [];
    
    private Guid _guidOnRoundStartPost = Guid.Empty;

    public void Initialize()
    {
        _guidOnRoundStartPost = core.GameEvent.HookPost<EventRoundStart>(OnRoundStartPost);
        eventSubscriber.OnEffectDestroyed += OnEffectDestroyed;
        eventSubscriber.OnEffectCreated += OnEffectCreated;
    }

    public void Dispose()
    {
        core.GameEvent.Unhook(_guidOnRoundStartPost);
        eventSubscriber.OnEffectDestroyed -= OnEffectDestroyed;
        eventSubscriber.OnEffectCreated -= OnEffectCreated;
    }
    
    public IEffect ApplyEffect<T>(IPlayer? caster, IPlayer target) where T : IEffect
    {
        return effectFactory.Create<T>(caster, target);
    }
    
    public void DestroyEffectByPlayer<T>(IPlayer target) where T : IEffect
    {
        var effect = _allEffects.Find(ef => ef is T && ef.Target.Equals(target));
        
        if (effect == null)
        {
            return;
        }
        
        effect.Destroy();
        _allEffects.Remove(effect);
    }
    
    public bool PlayerHasEffect<T>(IPlayer player) where T : IEffect
    {
        return _allEffects.Find(ef => ef is T && ef.Target.Equals(player)) != null;
    }
    
    private HookResult OnRoundStartPost(EventRoundStart @event)
    {
        DestroyAllEffects();
        
        return HookResult.Continue;
    }

    private void OnEffectDestroyed(IEffect effect)
    {
        _allEffects.Remove(effect);
    }

    private void OnEffectCreated(IEffect effect)
    {
        _allEffects.Add(effect);
    }
    
    private void DestroyAllEffects()
    {
        foreach (var effect in _allEffects)
        {
            effect.Destroy();
            _allEffects.Remove(effect);
        }
    }
}