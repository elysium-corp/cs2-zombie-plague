using Shop.Api.Data;
using Shop.Core.Data;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Application;

/// <summary>Проверяет доступность магазина и покупок для текущего состояния игрока.</summary>
internal interface IShopAccessEvaluator
{
    /// <summary>Возвращает сторону магазина по состоянию заражения игрока.</summary>
    ShopType GetShopType(IPlayer player);

    /// <summary>Проверяет возможность покупки оффера с необязательной заменой цены.</summary>
    ShopAvailability Evaluate(IPlayer player, ShopOfferDefinition offer, int? price = null);

    /// <summary>Проверяет доступность докупки патронов без лимитов покупки самого оружия.</summary>
    ShopAvailability EvaluateAmmo(IPlayer player, ShopOfferDefinition offer);
}
