using Common.Database.Storages;
using Common.Hooks.Abstractions;
using Economy.Api.Events;
using Economy.Core.Data.Configs;
using Economy.Core.Data.Store;
using Microsoft.Extensions.Options;
using MSApi.Exceptions;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Economy.Core.Services;

internal sealed class EconomyService(
    IOptions<EconomyConfig> config,
    PlayerSessionStore<PlayerAccountState> sessions,
    IHookPublisher hooks
) : IEconomyService
{
    public int GetBalance(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return sessions
                   .Get(player.SteamID)?
                   .Read(data => data.Balance)
               ?? 0;
    }

    public bool HasEnoughMoney(IPlayer player, int amount)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (amount == 0)
        {
            return true;
        }

        var session = sessions.Get(player.SteamID);

        if (session is null)
        {
            return false;
        }

        var snapshot = session.CreateSnapshot(data => data.Balance);

        if (!snapshot.IsLoaded)
        {
            return false;
        }

        return snapshot.Data >= amount;
    }

    public void GiveMoney(IPlayer player, int amount)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (amount == 0)
        {
            return;
        }

        var preContext = new EconomyTransactionProcessingContext(
            player,
            amount,
            EconomyTransactionKind.Credit
        );

        hooks.Dispatch(ref preContext);

        if (preContext.IsCancelled)
        {
            DispatchRejected(preContext, EconomyTransactionRejectionReason.Cancelled);
            return;
        }

        if (preContext.Amount < 0)
        {
            DispatchRejected(preContext, EconomyTransactionRejectionReason.InvalidAmount);
            return;
        }

        if (preContext.Amount == 0)
        {
            return;
        }

        var preparedPlayer = preContext.Player;
        var preparedAmount = preContext.Amount;
        var session = sessions.Get(preparedPlayer.SteamID);

        if (session is null)
        {
            DispatchRejected(preContext, EconomyTransactionRejectionReason.AccountUnavailable);
            return;
        }

        var previousBalance = 0;
        var newBalance = 0;

        var changed = session.TryUpdate(data =>
        {
            previousBalance = data.Balance;

            var balance = Math.Clamp(
                (long)data.Balance + preparedAmount,
                0L,
                config.Value.MaxMoney
            );

            newBalance = (int)balance;

            if (newBalance == data.Balance)
            {
                return false;
            }

            data.Balance = newBalance;

            return true;
        });

        if (!changed)
        {
            DispatchRejected(preContext, EconomyTransactionRejectionReason.BalanceLimitReached);
            return;
        }

        try
        {
            ApplyBalanceToGame(preparedPlayer, newBalance);
        }
        catch (Exception exception)
        {
            DispatchFailed(preparedPlayer, preparedAmount, EconomyTransactionKind.Credit, exception);
            throw;
        }

        var postContext = new EconomyTransactionCommittedContext(
            preparedPlayer,
            preparedAmount,
            newBalance - previousBalance,
            previousBalance,
            newBalance,
            EconomyTransactionKind.Credit
        );

        hooks.Dispatch(ref postContext);
    }

    public bool TrySpendMoney(IPlayer player, int amount)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (amount == 0)
        {
            return true;
        }

        var preContext = new EconomyTransactionProcessingContext(
            player,
            amount,
            EconomyTransactionKind.Debit
        );

        hooks.Dispatch(ref preContext);

        if (preContext.IsCancelled)
        {
            DispatchRejected(preContext, EconomyTransactionRejectionReason.Cancelled);
            return false;
        }

        if (preContext.Amount < 0)
        {
            DispatchRejected(preContext, EconomyTransactionRejectionReason.InvalidAmount);
            return false;
        }

        if (preContext.Amount == 0)
        {
            return true;
        }

        var preparedPlayer = preContext.Player;
        var preparedAmount = preContext.Amount;
        var session = sessions.Get(preparedPlayer.SteamID);

        if (session is null)
        {
            DispatchRejected(preContext, EconomyTransactionRejectionReason.AccountUnavailable);
            return false;
        }

        var snapshot = session.CreateSnapshot(data => data.Balance);

        if (!snapshot.IsLoaded)
        {
            DispatchRejected(preContext, EconomyTransactionRejectionReason.AccountNotLoaded);
            return false;
        }

        var previousBalance = 0;
        var newBalance = 0;

        var spent = session.TryUpdate(data =>
        {
            if (data.Balance < preparedAmount)
            {
                return false;
            }

            previousBalance = data.Balance;
            data.Balance -= preparedAmount;

            newBalance = data.Balance;

            return true;
        });

        if (!spent)
        {
            DispatchRejected(preContext, EconomyTransactionRejectionReason.InsufficientFunds);
            return false;
        }

        try
        {
            ApplyBalanceToGame(preparedPlayer, newBalance);
        }
        catch (Exception exception)
        {
            DispatchFailed(preparedPlayer, preparedAmount, EconomyTransactionKind.Debit, exception);
            throw;
        }

        var postContext = new EconomyTransactionCommittedContext(
            preparedPlayer,
            preparedAmount,
            previousBalance - newBalance,
            previousBalance,
            newBalance,
            EconomyTransactionKind.Debit
        );

        hooks.Dispatch(ref postContext);

        return true;
    }

    private void DispatchRejected(
        EconomyTransactionProcessingContext transaction,
        EconomyTransactionRejectionReason reason
    )
    {
        var context = new EconomyTransactionRejectedContext(
            transaction.Player,
            transaction.Amount,
            transaction.Kind,
            reason
        );

        hooks.Dispatch(ref context);
    }

    private void DispatchFailed(
        IPlayer player,
        int amount,
        EconomyTransactionKind kind,
        Exception exception
    )
    {
        var context = new EconomyTransactionFailedContext(player, amount, kind, exception);
        hooks.Dispatch(ref context);
    }

    private static void ApplyBalanceToGame(IPlayer player, int balance)
    {
        var moneyServices = GetMoneyServices(player);

        moneyServices.Account = balance;
        moneyServices.AccountUpdated();
    }

    private static CCSPlayerController_InGameMoneyServices GetMoneyServices(IPlayer player)
    {
        return player.Controller.InGameMoneyServices ?? throw new MoneyServicesNotFoundException("Player money services were not found!");
    }
}
