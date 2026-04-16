using Common.Effects.Contracts;
using Common.Effects.Effects;
using Common.Effects.Effects.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Common.Effects;

public sealed class EffectService : IEffectService
{
    private static volatile IEffectService? _instance;
    private static readonly Lock Lock = new();
    private readonly List<IEffect> _allEffects = [];

    private readonly IEffectFactory _effectFactory;

    internal EffectService(IEffectFactory effectFactory)
    {
        _effectFactory = effectFactory;
    }

    public TEffect? ApplyEffect<TEffect>(IPlayer? caster, IPlayer target, IEffectSettings? settings)
        where TEffect : class, IEffect
    {
        var effect = _effectFactory.Create<TEffect>(RemoveEffectCallback, caster, target, settings);

        if (effect == null) return null;

        effect.Start();
        _allEffects.Add(effect);

        return effect;
    }

    public void DestroyEffect<TEffect>(IPlayer target) where TEffect : IEffect
    {
        var effect = _allEffects.Find(ef => ef is TEffect && ef.Target.Equals(target));

        if (effect == null) return;

        Remove(effect);
    }

    public bool HasEffect<TEffect>(IPlayer player) where TEffect : IEffect
    {
        return _allEffects.Find(ef => ef is TEffect && ef.Target.Equals(player)) != null;
    }

    public void DestroyAllEffects()
    {
        foreach (var effect in _allEffects.ToArray()) Remove(effect);
    }

    public static IEffectService Provide(ISwiftlyCore core)
    {
        if (_instance == null)
            lock (Lock)
            {
                if (_instance == null)
                {
                    var factory = new EffectFactory(core);
                    _instance = new EffectService(factory);
                }
            }

        return _instance;
    }

    private void Remove(IEffect effect)
    {
        effect.Destroy();
        _allEffects.Remove(effect);
    }

    private void RemoveEffectCallback(IEffect effect)
    {
        _allEffects.Remove(effect);
    }
}