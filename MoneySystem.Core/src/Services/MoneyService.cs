using MSApi.Exceptions;
using SwiftlyS2.Shared.Players;

namespace MoneySystem.Core.Services;

internal sealed class MoneyService : IMoneyService
{
    public int GetMoney(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return player.Controller.InGameMoneyServices?.Account
               ?? throw new MoneyServicesNotFoundException("Money services were not found for the player.");
    }

    public void GiveMoney(IPlayer player, int amount)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (amount < 0)
        {
            throw new NegativeMoneyException("Money amount cannot be negative.");
        }
        
        var moneyServices = player.Controller.InGameMoneyServices;

        if (moneyServices == null)
        {
            throw new MoneyServicesNotFoundException("Money services were not found for the player.");
        }
        
        moneyServices.Account += amount;
        moneyServices.AccountUpdated();
    }

    public bool TrySpendMoney(IPlayer player, int amount)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (amount < 0)
        {
            throw new NegativeMoneyException("Money amount cannot be negative.");
        }

        var moneyServices = player.Controller.InGameMoneyServices;

        if (moneyServices == null)
        {
            throw new MoneyServicesNotFoundException("Money services were not found for the player.");
        }

        if (moneyServices.Account < amount)
        {
            return false;
        }

        moneyServices.Account -= amount;
        moneyServices.AccountUpdated();
        return true;
    }
}
