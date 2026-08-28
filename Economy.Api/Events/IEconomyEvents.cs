namespace Economy.Api.Events;

/// <summary>События экономики, сгруппированные по доменам.</summary>
public interface IEconomyEvents
{
    /// <summary>События денежных транзакций.</summary>
    IEconomyTransactionEvents Transactions { get; }

    /// <summary>События жизненного цикла денежных счетов.</summary>
    IEconomyAccountEvents Accounts { get; }
}
