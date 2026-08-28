using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>Причина ожидаемого отказа покупки боеприпасов.</summary>
public enum WeaponAmmoPurchaseRejectionReason
{
    /// <summary>Для оружия не настроена покупка боеприпасов.</summary>
    NotConfigured,

    /// <summary>Запас боеприпасов уже заполнен.</summary>
    ReserveFull,

    /// <summary>Покупка отменена обработчиком.</summary>
    Cancelled,

    /// <summary>Обработчик указал недопустимую цену или количество.</summary>
    InvalidValues,

    /// <summary>Экономика отклонила списание средств.</summary>
    PaymentRejected
}

/// <summary>Контекст модификации урона пользовательского оружия.</summary>
public struct WeaponDamageModifyingContext(
    IPlayer attacker,
    IPlayer victim,
    IWeapon weapon,
    float originalDamage,
    float damage
) : IPreHookContext
{
    /// <summary>Атакующий игрок.</summary>
    public IPlayer Attacker { get; } = attacker;

    /// <summary>Игрок, получающий урон.</summary>
    public IPlayer Victim { get; } = victim;

    /// <summary>Пользовательское оружие.</summary>
    public IWeapon Weapon { get; } = weapon;

    /// <summary>Урон до множителя пользовательского оружия.</summary>
    public float OriginalDamage { get; } = originalDamage;

    /// <summary>Рассчитанный урон. Может быть изменён обработчиком.</summary>
    public float Damage { get; set; } = damage;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст применённой модификации урона.</summary>
public readonly struct WeaponDamageModifiedContext(
    IPlayer attacker,
    IPlayer victim,
    IWeapon weapon,
    float originalDamage,
    float damage
) : IPostHookContext
{
    /// <summary>Атакующий игрок.</summary>
    public IPlayer Attacker { get; } = attacker;

    /// <summary>Игрок, получивший урон.</summary>
    public IPlayer Victim { get; } = victim;

    /// <summary>Пользовательское оружие.</summary>
    public IWeapon Weapon { get; } = weapon;

    /// <summary>Урон до модификации.</summary>
    public float OriginalDamage { get; } = originalDamage;

    /// <summary>Применённый урон.</summary>
    public float Damage { get; } = damage;
}

/// <summary>Контекст обработки попадания пули пользовательского оружия.</summary>
public struct WeaponImpactProcessingContext(IPlayer player, IWeapon weapon, Vector position) : IPreHookContext
{
    /// <summary>Стрелявший игрок.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Пользовательское оружие.</summary>
    public IWeapon Weapon { get; } = weapon;

    /// <summary>Позиция попадания, используемая эффектами.</summary>
    public Vector Position { get; set; } = position;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст обработанного попадания пули.</summary>
public readonly struct WeaponImpactProcessedContext(IPlayer player, IWeapon weapon, Vector position) : IPostHookContext
{
    /// <summary>Стрелявший игрок.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Пользовательское оружие.</summary>
    public IWeapon Weapon { get; } = weapon;

    /// <summary>Позиция использованных эффектов.</summary>
    public Vector Position { get; } = position;
}

/// <summary>Контекст покупки боеприпасов до списания денег.</summary>
public struct WeaponAmmoPurchasingContext(IPlayer player, IWeapon weapon, int price, int amount) : IPreHookContext
{
    /// <summary>Покупающий игрок.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Пользовательское оружие.</summary>
    public IWeapon Weapon { get; } = weapon;

    /// <summary>Цена покупки. Может быть изменена обработчиком.</summary>
    public int Price { get; set; } = price;

    /// <summary>Количество добавляемых патронов. Может быть изменено обработчиком.</summary>
    public int Amount { get; set; } = amount;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст успешной покупки боеприпасов.</summary>
public readonly struct WeaponAmmoPurchasedContext(
    IPlayer player,
    IWeapon weapon,
    int price,
    int amount,
    int reserveAmmo
) : IPostHookContext
{
    /// <summary>Покупающий игрок.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Пользовательское оружие.</summary>
    public IWeapon Weapon { get; } = weapon;

    /// <summary>Списанная цена.</summary>
    public int Price { get; } = price;

    /// <summary>Добавленное количество патронов.</summary>
    public int Amount { get; } = amount;

    /// <summary>Новый запас боеприпасов.</summary>
    public int ReserveAmmo { get; } = reserveAmmo;
}

/// <summary>Контекст ожидаемого отказа покупки боеприпасов.</summary>
public readonly struct WeaponAmmoPurchaseRejectedContext(
    IPlayer player,
    IWeapon weapon,
    WeaponAmmoPurchaseRejectionReason reason
) : IPostHookContext
{
    /// <summary>Покупающий игрок.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Пользовательское оружие.</summary>
    public IWeapon Weapon { get; } = weapon;

    /// <summary>Причина отказа.</summary>
    public WeaponAmmoPurchaseRejectionReason Reason { get; } = reason;
}
