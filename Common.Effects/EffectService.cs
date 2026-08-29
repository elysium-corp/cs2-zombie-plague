using System.Runtime.CompilerServices;
using Common.Effects.Contracts;
using Common.Effects.Effects;
using Common.Effects.Effects.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Common.Effects;

public sealed class EffectService : IEffectService, IDisposable
{
    private static readonly ConditionalWeakTable<ISwiftlyCore, EffectService> Instances = new();
    private readonly Lock _sync = new();
    private readonly List<IEffect> _effects = [];
    private readonly IEffectFactory _factory;
    private int _disposed;

    internal EffectService(IEffectFactory factory) => _factory = factory;

    public TEffect? ApplyEffect<TEffect>(IPlayer? caster, IPlayer target, IEffectSettings? settings = null)
        where TEffect : class, IEffect
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        lock (_sync)
        {
            if (_effects.Any(effect => effect is TEffect && effect.Target.Equals(target)))
            {
                return null;
            }

            var effect = _factory.Create<TEffect>(RemoveEffectCallback, caster, target, settings);
            if (effect is null)
            {
                return null;
            }

            var started = effect is BaseEffect baseEffect ? baseEffect.TryStart() : Start(effect);
            if (!started)
            {
                return null;
            }

            _effects.Add(effect);
            return effect;
        }
    }

    public void DestroyEffect<TEffect>(IPlayer target) where TEffect : IEffect
    {
        IEffect? effect;
        lock (_sync)
        {
            effect = _effects.FirstOrDefault(item => item is TEffect && item.Target.Equals(target));
            if (effect is not null) _effects.Remove(effect);
        }
        effect?.Destroy();
    }

    public bool HasEffect<TEffect>(IPlayer player) where TEffect : IEffect
    {
        lock (_sync) return _effects.Any(effect => effect is TEffect && effect.Target.Equals(player));
    }

    public void DestroyAllEffects()
    {
        IEffect[] snapshot;
        lock (_sync)
        {
            snapshot = _effects.ToArray();
            _effects.Clear();
        }
        foreach (var effect in snapshot) effect.Destroy();
    }

    public static IEffectService Provide(ISwiftlyCore core) =>
        Instances.GetValue(core, static value => new EffectService(new EffectFactory(value)));

    public static void Release(ISwiftlyCore core)
    {
        if (!Instances.TryGetValue(core, out var service)) return;
        Instances.Remove(core);
        service.Dispose();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        DestroyAllEffects();
    }

    private static bool Start(IEffect effect)
    {
        effect.Start();
        return true;
    }

    private void RemoveEffectCallback(IEffect effect)
    {
        lock (_sync) _effects.Remove(effect);
    }
}
