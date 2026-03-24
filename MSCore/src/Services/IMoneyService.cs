using SwiftlyS2.Shared.Players;

namespace MoneySystem.Services;

internal interface IMoneyService
{
    public void GiveMoney(IPlayer player, int amount);
}