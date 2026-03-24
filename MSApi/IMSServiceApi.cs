using SwiftlyS2.Shared.Players;

namespace MSApi;

public interface IMSServiceApi
{
    public void GiveMoney(IPlayer player, int amount);
    
    public static readonly string SharedApiKey = "MS.Core.IMSServiceApi";
}