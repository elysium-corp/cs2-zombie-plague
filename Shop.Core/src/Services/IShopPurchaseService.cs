using Shop.Api.Data;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Services;

internal interface IShopPurchaseService
{
    ShopPurchaseResult TryPurchase(IPlayer player, string itemId);
}
