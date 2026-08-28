using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api.Events.Contexts.Grenades;

/// <summary>Причина ожидаемого отказа обработки броска гранаты.</summary>
public enum GrenadeThrowRejectionReason
{
    /// <summary>Обработка отменена подписчиком.</summary>
    Cancelled,

    /// <summary>Сущность снаряда стала недействительной.</summary>
    InvalidProjectile
}

/// <summary>Причина ожидаемого отказа пользовательской детонации.</summary>
public enum GrenadeDetonationRejectionReason
{
    /// <summary>Детонация отменена подписчиком.</summary>
    Cancelled,

    /// <summary>Подписчик заменил гранату несовместимым объектом.</summary>
    InvalidGrenade,

    /// <summary>Сущность снаряда стала недействительной.</summary>
    InvalidProjectile,

    /// <summary>Не удалось определить действительного бросившего игрока.</summary>
    InvalidThrower
}

/// <summary>Контекст ожидаемого отказа обработки броска гранаты.</summary>
public readonly struct GrenadeThrowRejectedContext(
    IGrenade grenade,
    CBaseCSGrenadeProjectile projectile,
    GrenadeThrowRejectionReason reason
) : IPostHookContext
{
    /// <summary>Пользовательская граната.</summary>
    public IGrenade Grenade { get; } = grenade;

    /// <summary>Сущность снаряда.</summary>
    public CBaseCSGrenadeProjectile Projectile { get; } = projectile;

    /// <summary>Причина отказа.</summary>
    public GrenadeThrowRejectionReason Reason { get; } = reason;
}

/// <summary>Контекст ожидаемого отказа пользовательской детонации.</summary>
public readonly struct GrenadeDetonationRejectedContext(
    IGrenade grenade,
    CBaseCSGrenadeProjectile projectile,
    GrenadeDetonationRejectionReason reason
) : IPostHookContext
{
    /// <summary>Пользовательская граната.</summary>
    public IGrenade Grenade { get; } = grenade;

    /// <summary>Сущность снаряда.</summary>
    public CBaseCSGrenadeProjectile Projectile { get; } = projectile;

    /// <summary>Причина отказа.</summary>
    public GrenadeDetonationRejectionReason Reason { get; } = reason;
}
