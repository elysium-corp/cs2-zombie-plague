using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>Причина ожидаемого отказа покупки предмета.</summary>
public enum ItemPurchaseRejectionReason
{
    /// <summary>Покупка отменена обработчиком.</summary>
    Cancelled,

    /// <summary>Выбранный обработчиком игрок недействителен или мёртв.</summary>
    InvalidPlayer,

    /// <summary>Предмет недоступен текущей роли игрока.</summary>
    CannotUse,

    /// <summary>Экономика отклонила списание денег.</summary>
    PaymentRejected,

    /// <summary>Модуль снаряжения отклонил постановку выдачи.</summary>
    GrantRejected
}

/// <summary>Контекст подтверждённого списания денег за предмет.</summary>
public readonly struct ItemPaymentCommittedContext(IPlayer player, IShopItem item, int amount) : IPostHookContext
{
    /// <summary>Игрок, с которого списаны деньги.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Оплаченный предмет.</summary>
    public IShopItem Item { get; } = item;

    /// <summary>Списанная сумма.</summary>
    public int Amount { get; } = amount;
}

/// <summary>Контекст ожидаемого отказа покупки.</summary>
public readonly struct ItemPurchaseRejectedContext(
    IPlayer player,
    IShopItem item,
    ItemPurchaseRejectionReason reason
) : IPostHookContext
{
    /// <summary>Игрок, пытавшийся купить предмет.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Предмет, который пытались купить.</summary>
    public IShopItem Item { get; } = item;

    /// <summary>Причина отказа.</summary>
    public ItemPurchaseRejectionReason Reason { get; } = reason;
}

/// <summary>Контекст возврата денег после отклонённой выдачи.</summary>
public readonly struct ItemPaymentRefundedContext(IPlayer player, IShopItem item, int amount) : IPostHookContext
{
    /// <summary>Игрок, которому возвращены деньги.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Предмет, выдача которого была отклонена.</summary>
    public IShopItem Item { get; } = item;

    /// <summary>Сумма возврата.</summary>
    public int Amount { get; } = amount;
}
