using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Common.Effects.Effects.Contracts;

public abstract class BaseTickEffect(ISwiftlyCore core, Action<IEffect> callback, IPlayer? caster, IPlayer target) : BaseEffect(core, callback, caster, target)
{
    protected abstract float TickInterval { get; }
    
    private CancellationTokenSource? Token { get; set; }

    public override void Start()
    {
        CreateTickTimer();
        
        base.Start();
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

    private void CreateTickTimer()
    {
        Token = Core.Scheduler.DelayAndRepeatBySeconds(TickInterval, TickInterval, TryTick);
    }
    
    /// <summary>
    /// Функция вызывается каждый TickInterval
    /// </summary>
    protected virtual void TickEffect()
    {
    }

    private void DestroyThinker()
    {
        Token?.Cancel();
    }
}