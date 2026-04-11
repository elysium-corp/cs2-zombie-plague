using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Common.Effects.Effects.Contracts;

public abstract class BaseEffect(ISwiftlyCore core, IPlayer? caster, IPlayer target) : IEffect
{
    protected ISwiftlyCore Core => core;

    public IPlayer? Caster => caster;

    public IPlayer Target => target;
    
    protected CParticleSystem? Particle { get; set; }

    public abstract float Duration { get; }

    private CancellationTokenSource? DurationToken { get; set; }

    public abstract void Destroy();

    public virtual void Start()
    {
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
        DurationToken?.Cancel();
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
        
        StartDestroyTimer();

        ApplyEffect();
    }

    private void StartDestroyTimer()
    {
        DurationToken = Core.Scheduler.DelayBySeconds(Duration, DestroyEffect);
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