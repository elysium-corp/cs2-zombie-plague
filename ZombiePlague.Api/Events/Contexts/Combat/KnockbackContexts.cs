using Common.Hooks.Abstractions;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data;

namespace ZombiePlague.Api.Events.Contexts.Combat;

/// <summary>Контекст применения отбрасывания к зомби.</summary>
public struct KnockbackApplyingContext(
    IPlayer attacker,
    IPlayer victim,
    KnockbackData data,
    Vector velocity
) : IPreHookContext
{
    /// <summary>Атакующий игрок.</summary>
    public IPlayer Attacker { get; } = attacker;

    /// <summary>Отбрасываемый игрок.</summary>
    public IPlayer Victim { get; } = victim;

    /// <summary>Параметры расчёта отбрасывания.</summary>
    public KnockbackData Data { get; } = data;

    /// <summary>Рассчитанная итоговая скорость. Может быть изменена обработчиком.</summary>
    public Vector Velocity { get; set; } = velocity;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст применённого отбрасывания.</summary>
public readonly struct KnockbackAppliedContext(
    IPlayer attacker,
    IPlayer victim,
    KnockbackData data,
    Vector velocity
) : IPostHookContext
{
    /// <summary>Атакующий игрок.</summary>
    public IPlayer Attacker { get; } = attacker;

    /// <summary>Отброшенный игрок.</summary>
    public IPlayer Victim { get; } = victim;

    /// <summary>Параметры расчёта отбрасывания.</summary>
    public KnockbackData Data { get; } = data;

    /// <summary>Применённая скорость.</summary>
    public Vector Velocity { get; } = velocity;
}
