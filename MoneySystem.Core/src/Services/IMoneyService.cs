using SwiftlyS2.Shared.Players;

namespace MoneySystem.Core.Services;

internal interface IMoneyService
{
    public void GiveMoney(IPlayer player, int amount);
}