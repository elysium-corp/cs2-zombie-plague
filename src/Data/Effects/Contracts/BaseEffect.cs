using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Effects.Contracts;

public abstract class BaseEffect : ISoundPlayable
{
    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();
    private readonly EffectManager _effectManager = DependencyManager.GetService<EffectManager>();

    protected IPlayer? Caster { get; private set; }
    public IPlayer Target { get; private set; }

    public abstract float Duration { get; set; }
    private CancellationTokenSource? DurationThinker { get; set; }

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
    public virtual void DestroyEffect()
    {
        DurationThinker?.Cancel();
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
}