using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Common.Effects.Effects.Contracts;

public abstract class BaseEffect(ISwiftlyCore core, Action<IEffect> callback, IPlayer? caster, IPlayer target) : IEffect
{
    protected ISwiftlyCore Core => core;

    public IPlayer? Caster => caster;

    public IPlayer Target => target;
    
    protected CParticleSystem? Particle { get; set; }

    public abstract float Duration { get; }

    private CancellationTokenSource? DestroyDurationToken { get; set; }

    public abstract void Destroy();

    public virtual void Start()
    {
        TryApply();
    }

    /// <summary>
    /// Проверяет, можно ли применить эффект к цели в текущий момент.
    /// Вызывается перед <see cref="ApplyEffect"/>; если возвращает <c>false</c>,
    /// эффект не применяется и не добавляется к цели.
    /// </summary>
    /// <returns>
    /// <c>true</c>, если условия выполнены и эффект можно применить;
    /// иначе <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Базовая реализация всегда разрешает применение. Переопределяйте,
    /// чтобы добавить свои ограничения (например, проверку состояния игрока,
    /// иммунитета или несовместимых эффектов).
    /// </remarks>
    protected virtual bool CanApply() => true;

    /// <summary>
    /// Точка входа эффекта: вызывается один раз сразу после создания,
    /// когда <see cref="CanApply"/> вернул <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Здесь следует применять изменения к цели (визуал, скорость, урон и т.д.).
    /// Парная очистка выполняется в <see cref="DestroyEffect"/>.
    /// Базовая реализация пустая.
    /// </remarks>
    protected virtual void ApplyEffect() { }

    /// <summary>
    /// Завершает жизненный цикл эффекта
    /// и останавливает таймер длительности.
    /// </summary>
    /// <remarks>
    /// Вызывается при истечении длительности или при принудительном снятии.
    /// При переопределении обязательно вызывайте <c>base.DestroyEffect()</c>,
    /// иначе эффект не будет корректно удалён (не сработает <c>callback</c>
    /// и не отменится <see cref="DestroyDurationToken"/>).
    /// Здесь же откатывайте все изменения, сделанные в <see cref="ApplyEffect"/>.
    /// </remarks>
    protected virtual void DestroyEffect()
    {
        callback.Invoke(this);
        DestroyDurationToken?.Cancel();
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
        DestroyDurationToken = Core.Scheduler.DelayBySeconds(Duration, DestroyEffect);
    }

    protected virtual void PlaySound(string soundName)
    {
    }

    protected virtual void DestroyParticle()
    {
    }

    protected virtual void CreateParticle()
    {
    }
}