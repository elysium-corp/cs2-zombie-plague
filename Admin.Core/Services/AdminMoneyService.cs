using Economy.Api;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Services;

internal sealed class AdminMoneyService
{
    public IEconomyApi? Economy { get; set; }

    public int Give(IPlayer target, int amount)
    {
        if (amount <= 0 || target is not { IsValid: true, IsAuthorized: true, IsFakeClient: false } ||
            Economy is not { } economy)
            return 0;

        var before = economy.GetBalance(target);
        economy.GiveMoney(target, amount);
        return Math.Max(0, economy.GetBalance(target) - before);
    }
}
