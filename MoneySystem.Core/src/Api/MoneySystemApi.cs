using MoneySystem.Api;
using MoneySystem.Core.Services;
using SwiftlyS2.Shared.Players;

namespace MoneySystem.Core.Api;

internal sealed class MoneySystemApi(IMoneyService moneyService) : IMoneySystemApi
{
    public void GiveMoney(IPlayer player, int amount) => moneyService.GiveMoney(player, amount);
}