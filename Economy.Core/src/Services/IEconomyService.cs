using SwiftlyS2.Shared.Players;

namespace Economy.Core.Services;

internal interface IEconomyService
{
    public int GetBalance(IPlayer player);
    
    public void SetBalance(IPlayer player, int amount);

    public bool HasEnoughMoney(IPlayer player, int amount);
    
    public void GiveMoney(IPlayer player, int amount);
    
    public bool TrySpendMoney(IPlayer player, int amount);
}