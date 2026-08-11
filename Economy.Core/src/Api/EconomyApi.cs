using Economy.Api;
using Economy.Core.Services;
using SwiftlyS2.Shared.Players;

namespace Economy.Core.Api;

internal sealed class EconomyApi(IEconomyService economyService) : IEconomyApi
{
    public int GetBalance(IPlayer player)
    {
        return economyService.GetBalance(player);
    }

    public bool HasEnoughMoney(IPlayer player, int amount)
    {
        return economyService.HasEnoughMoney(player, amount);
    }

    public void GiveMoney(IPlayer player, int amount)
    {
        economyService.GiveMoney(player, amount);
    }
    
    public bool TrySpendMoney(IPlayer player, int amount)
    {
        return economyService.TrySpendMoney(player, amount);
    }
}