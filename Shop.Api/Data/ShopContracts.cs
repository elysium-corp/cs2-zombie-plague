namespace Shop.Api.Data;

/// <summary>Сторона магазина, автоматически выбранная по состоянию заражения.</summary>
public enum ShopType
{
    /// <summary>Магазин людей.</summary>
    Human,

    /// <summary>Магазин зомби.</summary>
    Zombie
}

/// <summary>Способ проверки требуемых привилегий оффера.</summary>
public enum ShopAccessMode
{
    /// <summary>Оффер доступен всем игрокам.</summary>
    Everyone,

    /// <summary>Достаточно любой из перечисленных привилегий.</summary>
    Any,

    /// <summary>Необходимы все перечисленные привилегии.</summary>
    All
}

/// <summary>Причина недоступности товара.</summary>
public enum ShopAvailabilityReason
{
    /// <summary>Товар доступен.</summary>
    Available,

    /// <summary>Магазин или товар отключён.</summary>
    Disabled,

    /// <summary>Поставщик предмета не найден.</summary>
    ProductUnavailable,

    /// <summary>Предмет недоступен для текущей стороны игрока.</summary>
    TeamUnavailable,

    /// <summary>Игроку не хватает требуемых привилегий.</summary>
    AccessDenied,

    /// <summary>Счёт игрока не загружен или средств недостаточно.</summary>
    InsufficientFunds,

    /// <summary>Исчерпан лимит покупок за раунд.</summary>
    RoundLimitReached,

    /// <summary>Исчерпан лимит покупок за карту.</summary>
    MapLimitReached,

    /// <summary>Ещё не истёк интервал до повторной покупки.</summary>
    CooldownActive,

    /// <summary>Игрок или его персонаж сейчас не может совершать покупку.</summary>
    InvalidPlayer,

    /// <summary>Покупка отменена обработчиком события.</summary>
    Cancelled,

    /// <summary>Списание средств отклонено.</summary>
    PaymentRejected,

    /// <summary>Предмет не удалось выдать; списание возвращено.</summary>
    GrantRejected,

    /// <summary>После ошибки выдачи не удалось вернуть списанные средства.</summary>
    RefundFailed,

    /// <summary>Для активного оружия не настроена покупка патронов.</summary>
    AmmoNotConfigured,

    /// <summary>Резерв патронов уже заполнен.</summary>
    AmmoFull
}

/// <summary>Неподвижное представление оффера для внешних модулей.</summary>
public sealed record ShopOffer(
    long Id,
    ShopType ShopType,
    string ProviderKey,
    string ItemKey,
    string DisplayNameKey,
    long? CategoryId,
    int Price,
    int? AmmoPrice,
    int AmmoAmount,
    int MaxPurchasesPerRound,
    int MaxPurchasesPerMap,
    int CooldownSeconds,
    ShopAccessMode AccessMode,
    IReadOnlySet<string> RequiredPrivileges,
    bool Enabled,
    int SortOrder
);

/// <summary>Результат проверки доступности оффера.</summary>
public readonly record struct ShopAvailability(
    bool Allowed,
    ShopAvailabilityReason Reason,
    TimeSpan RemainingCooldown
)
{
    /// <summary>Создаёт успешный результат.</summary>
    public static ShopAvailability Available() =>
        new(true, ShopAvailabilityReason.Available, TimeSpan.Zero);

    /// <summary>Создаёт отказ с указанной причиной.</summary>
    public static ShopAvailability Rejected(
        ShopAvailabilityReason reason,
        TimeSpan remainingCooldown = default) =>
        new(false, reason, remainingCooldown);
}
