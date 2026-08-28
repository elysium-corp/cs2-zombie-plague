using Economy.Api;
using Economy.Api.Events;
using Economy.Core.Services;
using SwiftlyS2.Shared.Players;

namespace Economy.Core.Api;

internal sealed class EconomyApi(IEconomyService economyService, IEconomyEvents events) : IEconomyApi
{
    public IEconomyEvents Events => events;

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
