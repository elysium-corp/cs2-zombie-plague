using Common.Hooks.Abstractions;
using Shop.Api.Data;
using SwiftlyS2.Shared.Players;

namespace Shop.Api.Events;

/// <summary>Контекст покупки до списания средств и выдачи предмета.</summary>
public struct ShopPurchasingContext(
    IPlayer player,
    ShopOffer offer,
    int price
) : IPreHookContext
{
    /// <summary>Игрок, совершающий покупку.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Выбранный оффер.</summary>
    public ShopOffer Offer { get; } = offer;

    /// <summary>Цена покупки; обработчик может изменить её.</summary>
    public int Price { get; set; } = price;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст успешно выданного и оплаченного товара.</summary>
public readonly struct ShopPurchasedContext(
    IPlayer player,
    ShopOffer offer,
    int price
) : IPostHookContext
{
    /// <summary>Игрок, получивший товар.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Купленный оффер.</summary>
    public ShopOffer Offer { get; } = offer;

    /// <summary>Фактически списанная цена.</summary>
    public int Price { get; } = price;
}

/// <summary>Контекст ожидаемого отказа покупки.</summary>
public readonly struct ShopPurchaseRejectedContext(
    IPlayer player,
    ShopOffer? offer,
    ShopAvailabilityReason reason
) : IPostHookContext
{
    /// <summary>Игрок, для которого отклонена покупка.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Оффер либо <c>null</c>, если он уже отсутствует в snapshot.</summary>
    public ShopOffer? Offer { get; } = offer;

    /// <summary>Причина отказа.</summary>
    public ShopAvailabilityReason Reason { get; } = reason;
}

/// <summary>Контекст успешно купленных патронов.</summary>
public readonly struct ShopAmmoPurchasedContext(
    IPlayer player,
    ShopOffer offer,
    int price,
    int addedAmount,
    int reserveAmmo
) : IPostHookContext
{
    /// <summary>Игрок, купивший патроны.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Оффер активного оружия.</summary>
    public ShopOffer Offer { get; } = offer;

    /// <summary>Фактически списанная цена.</summary>
    public int Price { get; } = price;

    /// <summary>Фактически добавленное количество патронов.</summary>
    public int AddedAmount { get; } = addedAmount;

    /// <summary>Новый резерв патронов.</summary>
    public int ReserveAmmo { get; } = reserveAmmo;
}
