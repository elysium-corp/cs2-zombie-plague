using SwiftlyS2.Shared.Players;

namespace MoneySystem.Core.Services;

internal interface IMoneyService
{
    int GetMoney(IPlayer player);

    void GiveMoney(IPlayer player, int amount);

    bool TrySpendMoney(IPlayer player, int amount);
}
