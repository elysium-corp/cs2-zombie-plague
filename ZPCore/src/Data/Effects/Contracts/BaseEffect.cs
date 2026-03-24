using ZPCore.Data.Abilities.Contracts;
using ZPCore.Data.Managers;
using ZPCore.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZPApi.Data;
using ZPApi.Events;

namespace ZPCore.Data.Effects.Contracts;

internal abstract class BaseEffect : IEffect, ISoundPlayable, IParticleRestricted
{
    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();
    private readonly EffectManager _effectManager = DependencyManager.GetService<EffectManager>();
    private readonly IEventPublisher _eventPublisher = DependencyManager.GetService<IEventPublisher>();
    
    public CParticleSystem? Particle { get; set; }
    public IPlayer? Caster { get; }
    public IPlayer Target { get; }

    public abstract float Duration { get; }

    private CancellationTokenSource? DurationThinker { get; set; }

    public abstract void Destroy();

    protected BaseEffect(IPlayer? caster, IPlayer target)
    {
        Caster = caster;
        Target = target;

        TryApply();
    }

    /// <summary>
    /// Условия для применения эффекта.
    /// </summary>
    protected virtual bool CanApply() => true;

    /// <summary>
    /// Вызывается у всех эффектов единожды, сразу после создания.
    /// </summary>
    protected virtual void ApplyEffect()
    {
    }

    /// <summary>
    /// Вызывается в конце жизни эффекта.
    /// </summary>
    protected virtual void DestroyEffect()
    {
        DurationThinker?.Cancel();
        _eventPublisher.OnEffectDestroyed(this);
    }

    private void TryApply()
    {
        if (!Target.IsValid || !Target.IsAlive)
        {
            return;
        }

        if (!CanApply())
        {
            return;
        }

        _effectManager.AddEffect(this);

        StartDestroyTimer();

        ApplyEffect();
    }

    private void StartDestroyTimer()
    {
        DurationThinker = _core.Scheduler.DelayBySeconds(Duration, DestroyEffect);
    }

    public virtual void PlaySound(string soundName)
    {
    }

    public virtual void DestroyParticle()
    {
    }

    public virtual void CreateParticle()
    {
    }
}