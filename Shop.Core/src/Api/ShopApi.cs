using Shop.Api;
using Shop.Api.Data;
using Shop.Api.Events;
using Shop.Core.Application;
using Shop.Core.Menus;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Api;

internal sealed class ShopApi(
    ShopMenu menu,
    ShopPurchaseService purchases,
    IShopEvents events) : IShopApi
{
    public IShopEvents Events => events;

    public void Open(IPlayer player) => menu.Open(player);

    public IReadOnlyCollection<ShopOffer> GetOffers(ShopType shopType) =>
        purchases.GetOffers(shopType);

    public ShopAvailability GetAvailability(IPlayer player, long offerId) =>
        purchases.GetAvailability(player, offerId);

    public bool TryPurchase(IPlayer player, long offerId) =>
        purchases.TryPurchase(player, offerId);

    public bool TryPurchaseActiveWeaponAmmo(IPlayer player) =>
        purchases.TryPurchaseActiveWeaponAmmo(player);
}
