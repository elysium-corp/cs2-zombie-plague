using Common.Effects.Events;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Common.Effects.Effects.Contracts;

internal abstract class BaseEffect : IEffect
{
    protected ISwiftlyCore Core { get; private init; }
    
    protected IEventPublisher EventPublisher { get; private init; }
    
    public CParticleSystem? Particle { get; set; }
    public IPlayer? Caster { get; }
    public IPlayer Target { get; }

    public abstract float Duration { get; }

    private CancellationTokenSource? DurationThinker { get; set; }

    public abstract void Destroy();

    protected BaseEffect(ISwiftlyCore core, IEventPublisher eventPublisher, IPlayer? caster, IPlayer target)
    {
        Core = core;
        EventPublisher = eventPublisher;
        
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
        EventPublisher.OnEffectDestroyed(this);
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
        
        EventPublisher.OnEffectCreated(this);

        StartDestroyTimer();

        ApplyEffect();
    }

    private void StartDestroyTimer()
    {
        DurationThinker = Core.Scheduler.DelayBySeconds(Duration, DestroyEffect);
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