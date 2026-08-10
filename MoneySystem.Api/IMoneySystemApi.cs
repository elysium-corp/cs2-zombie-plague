using SwiftlyS2.Shared.Players;

namespace MoneySystem.Api;

public interface IMoneySystemApi
{
    public void GiveMoney(IPlayer player, int amount);
    
    public static readonly string SharedApiKey = "MoneySystem.Api.IMoneySystemApi";
}
