using MoneySystem.Api;
using MoneySystem.Core.Services;
using SwiftlyS2.Shared.Players;

namespace MoneySystem.Core.Api;

internal sealed class MoneySystemApi(IMoneyService moneyService) : IMoneySystemPaymentApi
{
    public int GetMoney(IPlayer player) => moneyService.GetMoney(player);

    public void GiveMoney(IPlayer player, int amount) => moneyService.GiveMoney(player, amount);

    public bool TrySpendMoney(IPlayer player, int amount) => moneyService.TrySpendMoney(player, amount);
}
