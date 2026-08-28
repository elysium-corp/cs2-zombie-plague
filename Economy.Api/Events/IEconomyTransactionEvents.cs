using Common.Hooks.Abstractions;

namespace Economy.Api.Events;

/// <summary>События начисления и списания средств.</summary>
public interface IEconomyTransactionEvents
{
    /// <summary>Вызывается перед изменением баланса; игрока, сумму или саму операцию можно изменить.</summary>
    IHookSubscription<EconomyTransactionProcessingContext> Processing { get; }

    /// <summary>Вызывается после изменения сессии счёта и игровой проекции баланса.</summary>
    IHookSubscription<EconomyTransactionCommittedContext> Committed { get; }

    /// <summary>Вызывается при ожидаемом отказе без изменения баланса.</summary>
    IHookSubscription<EconomyTransactionRejectedContext> Rejected { get; }

    /// <summary>Вызывается при ошибке обновления игровой проекции после изменения сессии счёта.</summary>
    IHookSubscription<EconomyTransactionFailedContext> Failed { get; }
}
