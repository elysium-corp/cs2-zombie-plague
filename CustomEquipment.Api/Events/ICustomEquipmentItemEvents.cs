using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events.Contexts.Items;

namespace CustomEquipment.Api.Events;

/// <summary>
/// События покупок и общей выдачи пользовательских предметов.
/// </summary>
public interface ICustomEquipmentItemEvents
{
    /// <summary>Вызывается до списания денег за предмет. Покупку можно изменить или отменить.</summary>
    IHookSubscription<ItemPurchasingContext> Purchasing { get; }

    /// <summary>Вызывается непосредственно после подтверждённого списания денег.</summary>
    IHookSubscription<ItemPaymentCommittedContext> PaymentCommitted { get; }

    /// <summary>Вызывается после списания денег и успешной постановки выдачи предмета в очередь.</summary>
    IHookSubscription<ItemPurchasedContext> Purchased { get; }

    /// <summary>Вызывается при ожидаемом отказе покупки.</summary>
    IHookSubscription<ItemPurchaseRejectedContext> PurchaseRejected { get; }

    /// <summary>Вызывается после возврата денег из-за отклонённой выдачи.</summary>
    IHookSubscription<ItemPaymentRefundedContext> PaymentRefunded { get; }

    /// <summary>Вызывается непосредственно перед выдачей любого пользовательского предмета.</summary>
    IHookSubscription<ItemGivingContext> Giving { get; }

    /// <summary>Вызывается после завершения выдачи любого пользовательского предмета.</summary>
    IHookSubscription<ItemGivenContext> Given { get; }

    /// <summary>Вызывается при ожидаемом отказе выдачи предмета.</summary>
    IHookSubscription<ItemGiveRejectedContext> GiveRejected { get; }

    /// <summary>Вызывается при технической ошибке создания или постановки выдачи.</summary>
    IHookSubscription<ItemGiveFailedContext> GiveFailed { get; }
}
