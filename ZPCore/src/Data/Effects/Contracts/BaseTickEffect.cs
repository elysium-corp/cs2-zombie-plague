using ZPCore.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Effects.Contracts;

internal abstract class BaseTickEffect : BaseEffect
{
    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();
    public abstract float TickInterval { get; set; }
    private CancellationTokenSource? ThinkerToken { get; set; }

    protected BaseTickEffect(IPlayer? caster, IPlayer target) : base(caster, target)
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
        ThinkerToken = _core.Scheduler.DelayAndRepeatBySeconds(TickInterval, TickInterval, TryTick);
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