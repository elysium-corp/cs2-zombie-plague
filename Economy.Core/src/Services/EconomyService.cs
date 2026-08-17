using Common.Database.Storages;
using Economy.Core.Data.Configs;
using Economy.Core.Data.Store;
using Microsoft.Extensions.Options;
using MSApi.Exceptions;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Economy.Core.Services;

internal sealed class EconomyService(
    IOptions<EconomyConfig> config,
    PlayerSessionStore<PlayerAccountState> sessions
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

        var session = sessions.Get(player.SteamID);

        if (session is null)
        {
            return;
        }

        var newBalance = 0;

        var changed = session.TryUpdate(data =>
        {
            var balance = Math.Clamp(
                (long)data.Balance + amount,
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
            return;
        }

        ApplyBalanceToGame(player, newBalance);
    }

    public bool TrySpendMoney(IPlayer player, int amount)
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

        var newBalance = 0;

        var spent = session.TryUpdate(data =>
        {
            if (data.Balance < amount)
            {
                return false;
            }

            data.Balance -= amount;

            newBalance = data.Balance;

            return true;
        });

        if (!spent)
        {
            return false;
        }

        ApplyBalanceToGame(player, newBalance);

        return true;
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