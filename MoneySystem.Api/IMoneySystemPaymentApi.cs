using SwiftlyS2.Shared.Players;

namespace MoneySystem.Api;

public interface IMoneySystemPaymentApi : IMoneySystemApi
{
    int GetMoney(IPlayer player);

    bool TrySpendMoney(IPlayer player, int amount);

    new static readonly string SharedApiKey = "MoneySystem.Api.IMoneySystemPaymentApi";
}
