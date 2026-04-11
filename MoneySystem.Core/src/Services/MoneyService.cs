using MSApi.Exceptions;
using SwiftlyS2.Shared.Players;

namespace MoneySystem.Core.Services;

internal sealed class MoneyService : IMoneyService
{
    public void GiveMoney(IPlayer player, int amount)
    {
        if (amount < 0) throw new NegativeMoneyException("MSServiceApi: amount cannot be negative (>= 0)");
        
        var moneyServices = player.Controller.InGameMoneyServices;

        if (moneyServices == null) throw new MoneyServicesNotFoundException("MSServiceApi: MoneyServices not found!");
        
        var account = moneyServices.Account;

        moneyServices.Account = account + amount;
        moneyServices.AccountUpdated();
    }
}