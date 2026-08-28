using Common.Hooks.Abstractions;
using SwiftlyS2.Shared.Players;

namespace Economy.Api.Events;

/// <summary>Тип изменения денежного баланса.</summary>
public enum EconomyTransactionKind
{
    /// <summary>Начисление средств.</summary>
    Credit,

    /// <summary>Списание средств.</summary>
    Debit
}

/// <summary>Причина ожидаемого отказа денежной транзакции.</summary>
public enum EconomyTransactionRejectionReason
{
    /// <summary>Операция отменена обработчиком события.</summary>
    Cancelled,

    /// <summary>Обработчик события указал недопустимую отрицательную сумму.</summary>
    InvalidAmount,

    /// <summary>Для игрока ещё не создана сессия счёта.</summary>
    AccountUnavailable,

    /// <summary>Счёт ещё не загружен из базы данных.</summary>
    AccountNotLoaded,

    /// <summary>На счёте недостаточно средств.</summary>
    InsufficientFunds,

    /// <summary>Баланс уже достиг настроенного верхнего лимита.</summary>
    BalanceLimitReached
}

/// <summary>Контекст денежной транзакции до изменения баланса.</summary>
public struct EconomyTransactionProcessingContext(
    IPlayer player,
    int amount,
    EconomyTransactionKind kind
) : IPreHookContext
{
    /// <summary>Изначальный игрок.</summary>
    public IPlayer OriginalPlayer { get; } = player;

    /// <summary>Игрок, чей баланс будет изменён. Может быть заменён обработчиком.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Изначально запрошенная сумма.</summary>
    public int OriginalAmount { get; } = amount;

    /// <summary>Запрошенная сумма. Может быть изменена обработчиком.</summary>
    public int Amount { get; set; } = amount;

    /// <summary>Тип транзакции.</summary>
    public EconomyTransactionKind Kind { get; } = kind;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст успешно зафиксированной денежной транзакции.</summary>
public readonly struct EconomyTransactionCommittedContext(
    IPlayer player,
    int requestedAmount,
    int appliedAmount,
    int previousBalance,
    int balance,
    EconomyTransactionKind kind
) : IPostHookContext
{
    /// <summary>Игрок, чей баланс изменён.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Сумма после обработки события <c>Processing</c>.</summary>
    public int RequestedAmount { get; } = requestedAmount;

    /// <summary>Фактически начисленная или списанная сумма.</summary>
    public int AppliedAmount { get; } = appliedAmount;

    /// <summary>Баланс до транзакции.</summary>
    public int PreviousBalance { get; } = previousBalance;

    /// <summary>Баланс после транзакции.</summary>
    public int Balance { get; } = balance;

    /// <summary>Тип транзакции.</summary>
    public EconomyTransactionKind Kind { get; } = kind;
}

/// <summary>Контекст ожидаемого отказа денежной транзакции.</summary>
public readonly struct EconomyTransactionRejectedContext(
    IPlayer player,
    int amount,
    EconomyTransactionKind kind,
    EconomyTransactionRejectionReason reason
) : IPostHookContext
{
    /// <summary>Игрок, для которого выполнялась операция.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Сумма операции после обработки события.</summary>
    public int Amount { get; } = amount;

    /// <summary>Тип транзакции.</summary>
    public EconomyTransactionKind Kind { get; } = kind;

    /// <summary>Причина отказа.</summary>
    public EconomyTransactionRejectionReason Reason { get; } = reason;
}

/// <summary>Контекст технической ошибки после изменения сохранённого баланса.</summary>
public readonly struct EconomyTransactionFailedContext(
    IPlayer player,
    int amount,
    EconomyTransactionKind kind,
    Exception exception
) : IPostHookContext
{
    /// <summary>Игрок, для которого выполнялась операция.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Сумма операции.</summary>
    public int Amount { get; } = amount;

    /// <summary>Тип транзакции.</summary>
    public EconomyTransactionKind Kind { get; } = kind;

    /// <summary>Исключение, которое будет выброшено повторно после события.</summary>
    public Exception Exception { get; } = exception;
}
