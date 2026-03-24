using MoneySystem.Services;
using MSApi;
using SwiftlyS2.Shared.Players;

namespace MoneySystem.Api;

internal sealed class MSServiceApi(IMoneyService moneyService) : IMSServiceApi
{
    public void GiveMoney(IPlayer player, int amount) => moneyService.GiveMoney(player, amount);
}