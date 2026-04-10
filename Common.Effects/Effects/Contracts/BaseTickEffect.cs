using Common.Effects.Events;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Common.Effects.Effects.Contracts;

internal abstract class BaseTickEffect : BaseEffect
{
    public abstract float TickInterval { get; set; }
    private CancellationTokenSource? ThinkerToken { get; set; }

    protected BaseTickEffect(ISwiftlyCore core, IEventPublisher eventPublisher, IPlayer? caster, IPlayer target) : base(core, eventPublisher, caster, target)
    {
        CreateTickThinker();
    }
    
    protected override void DestroyEffect()
    {
        base.DestroyEffect();
        DestroyThinker();
    }

    private void TryTick()
    {
        (Target.IsValid && Target.IsAlive ? (Action)Tick : DestroyEffect)();
    }

    private void Tick()
    {
        TickEffect();
    }

    private void CreateTickThinker()
    {
        ThinkerToken = Core.Scheduler.DelayAndRepeatBySeconds(TickInterval, TickInterval, TryTick);
    }
    
    /// <summary>
    /// Функция вызывается каждый TickInterval
    /// </summary>
    protected virtual void TickEffect()
    {
    }

    private void DestroyThinker()
    {
        ThinkerToken?.Cancel();
    }
}