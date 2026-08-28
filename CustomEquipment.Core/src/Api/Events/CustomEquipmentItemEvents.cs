using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Items;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentItemEvents(IHookSubscriber hooks) : ICustomEquipmentItemEvents
{
    public IHookSubscription<ItemPurchasingContext> Purchasing { get; } =
        new HookEvent<ItemPurchasingContext>(hooks);

    public IHookSubscription<ItemPurchasedContext> Purchased { get; } =
        new HookEvent<ItemPurchasedContext>(hooks);

    public IHookSubscription<ItemPaymentCommittedContext> PaymentCommitted { get; } =
        new HookEvent<ItemPaymentCommittedContext>(hooks);

    public IHookSubscription<ItemPurchaseRejectedContext> PurchaseRejected { get; } =
        new HookEvent<ItemPurchaseRejectedContext>(hooks);

    public IHookSubscription<ItemPaymentRefundedContext> PaymentRefunded { get; } =
        new HookEvent<ItemPaymentRefundedContext>(hooks);

    public IHookSubscription<ItemGivingContext> Giving { get; } =
        new HookEvent<ItemGivingContext>(hooks);

    public IHookSubscription<ItemGivenContext> Given { get; } =
        new HookEvent<ItemGivenContext>(hooks);

    public IHookSubscription<ItemGiveRejectedContext> GiveRejected { get; } =
        new HookEvent<ItemGiveRejectedContext>(hooks);

    public IHookSubscription<ItemGiveFailedContext> GiveFailed { get; } =
        new HookEvent<ItemGiveFailedContext>(hooks);
}
