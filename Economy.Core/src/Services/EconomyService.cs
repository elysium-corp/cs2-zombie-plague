using Economy.Core.Data.Configs;
using Microsoft.Extensions.Options;
using MSApi.Exceptions;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Economy.Core.Services;

internal sealed class EconomyService(IOptions<EconomyConfig> config) : IEconomyService
{
    public int GetBalance(IPlayer player)
    {
        return GetMoneyServices(player).Account;
    }

    public void SetBalance(IPlayer player, int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var moneyServices = GetMoneyServices(player);

        moneyServices.Account = amount;
        moneyServices.AccountUpdated();
    }

    public bool HasEnoughMoney(IPlayer player, int amount)
    {
        return GetMoneyServices(player).Account >= amount;
    }

    public void GiveMoney(IPlayer player, int amount)
    {
        if (amount == 0) return;

        var moneyServices = GetMoneyServices(player);

        if (moneyServices.Account + amount >= config.Value.MaxMoney)
        {
            SetBalance(player, config.Value.MaxMoney);
            return;
        }

        SetBalance(player, moneyServices.Account + amount);
    }

    public bool TrySpendMoney(IPlayer player, int amount)
    {
        if (amount == 0)
        {
            return true;
        }

        var moneyServices = GetMoneyServices(player);

        if (moneyServices.Account < amount)
        {
            return false;
        }

        SetBalance(player, moneyServices.Account - amount);

        return true;
    }

    private static CCSPlayerController_InGameMoneyServices GetMoneyServices(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return player.Controller.InGameMoneyServices ??
               throw new MoneyServicesNotFoundException("Player money services were not found!");
    }
}