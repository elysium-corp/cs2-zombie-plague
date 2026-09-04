using Common.Hooks;
using Common.Hooks.Abstractions;
using Shop.Api.Events;

namespace Shop.Core.Application;

internal sealed class ShopEvents(IHookSubscriber hooks) : IShopEvents
{
    public IHookSubscription<ShopPurchasingContext> Purchasing { get; } =
        new HookEvent<ShopPurchasingContext>(hooks);

    public IHookSubscription<ShopPurchasedContext> Purchased { get; } =
        new HookEvent<ShopPurchasedContext>(hooks);

    public IHookSubscription<ShopPurchaseRejectedContext> PurchaseRejected { get; } =
        new HookEvent<ShopPurchaseRejectedContext>(hooks);

    public IHookSubscription<ShopAmmoPurchasedContext> AmmoPurchased { get; } =
        new HookEvent<ShopAmmoPurchasedContext>(hooks);
}
