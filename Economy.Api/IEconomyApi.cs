using SwiftlyS2.Shared.Players;

namespace Economy.Api;

public interface IEconomyApi
{
    public int GetBalance(IPlayer player);

    public bool HasEnoughMoney(IPlayer player, int amount);
    
    public void GiveMoney(IPlayer player, int amount);
    
    public bool TrySpendMoney(IPlayer player, int amount);
    
    public static readonly string SharedApiKey = "Economy.Api.IEconomyApi";
}