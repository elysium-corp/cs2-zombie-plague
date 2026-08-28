using Common.Hooks.Abstractions;
using ZombiePlague.Api.Data.Rounds;

namespace ZombiePlague.Api.Events.Contexts.Round;

/// <summary>Причина ожидаемого отказа в запуске режима раунда.</summary>
public enum RoundStartRejectionReason
{
    /// <summary>Сервер не находится в фазе подготовки.</summary>
    NotPreparing,

    /// <summary>Режим не удовлетворяет условиям запуска.</summary>
    CannotStart,

    /// <summary>Запуск отменён обработчиком события.</summary>
    Cancelled
}

/// <summary>Контекст начала подготовки к режиму Zombie Plague.</summary>
public struct RoundPreparingContext : IPreHookContext
{
    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст запущенного обратного отсчёта до старта режима.</summary>
public readonly struct RoundPreparedContext(int delaySeconds) : IPostHookContext
{
    /// <summary>Начальная продолжительность обратного отсчёта в секундах.</summary>
    public int DelaySeconds { get; } = delaySeconds;
}

/// <summary>Контекст завершения активного режима раунда.</summary>
public struct RoundEndingContext(IRound round) : IPreHookContext
{
    /// <summary>Завершаемый режим.</summary>
    public IRound Round { get; } = round;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст завершённого режима раунда.</summary>
public readonly struct RoundEndedContext(IRound round) : IPostHookContext
{
    /// <summary>Завершённый режим.</summary>
    public IRound Round { get; } = round;
}

/// <summary>Контекст постановки режима в очередь на следующий раунд.</summary>
public struct RoundSchedulingContext(IRound round) : IPreHookContext
{
    /// <summary>Режим, который требуется поставить в очередь.</summary>
    public IRound Round { get; } = round;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст режима, поставленного в очередь на следующий раунд.</summary>
public readonly struct RoundScheduledContext(IRound round) : IPostHookContext
{
    /// <summary>Режим, поставленный в очередь.</summary>
    public IRound Round { get; } = round;
}

/// <summary>Контекст очистки выбранного следующего режима.</summary>
public struct RoundScheduleClearingContext(IRound round) : IPreHookContext
{
    /// <summary>Режим, удаляемый из очереди.</summary>
    public IRound Round { get; } = round;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст очищенной очереди следующего режима.</summary>
public readonly struct RoundScheduleClearedContext(IRound round) : IPostHookContext
{
    /// <summary>Режим, удалённый из очереди.</summary>
    public IRound Round { get; } = round;
}

/// <summary>Контекст ожидаемого отказа в запуске режима.</summary>
public readonly struct RoundStartRejectedContext(string? roundId, RoundStartRejectionReason reason) : IPostHookContext
{
    /// <summary>Идентификатор режима, если он был известен.</summary>
    public string? RoundId { get; } = roundId;

    /// <summary>Причина отказа.</summary>
    public RoundStartRejectionReason Reason { get; } = reason;
}

/// <summary>Контекст технической ошибки при запуске режима.</summary>
public readonly struct RoundStartFailedContext(IRound round, Exception exception) : IPostHookContext
{
    /// <summary>Режим, запуск которого завершился ошибкой.</summary>
    public IRound Round { get; } = round;

    /// <summary>Перехваченное исключение. После события оно будет выброшено повторно.</summary>
    public Exception Exception { get; } = exception;
}
