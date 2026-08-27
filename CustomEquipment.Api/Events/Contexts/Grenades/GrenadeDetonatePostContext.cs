using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api.Events.Contexts.Grenades;

/// <summary>
/// Контекст после пользовательской детонации гранаты.
/// </summary>
public struct GrenadeDetonatePostContext(IGrenade grenade, CBaseCSGrenadeProjectile projectile, Vector position) : IPostHookContext
{
    /// <summary>Детонировавшая граната.</summary>
    public IGrenade Grenade { get; set; } = grenade;

    /// <summary>Сущность снаряда.</summary>
    public CBaseCSGrenadeProjectile Projectile { get; set; } = projectile;

    /// <summary>Позиция детонации.</summary>
    public Vector Position { get; set; } = position;
}
