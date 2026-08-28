using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api.Events.Contexts.Grenades;

/// <summary>
/// Контекст после обработки броска гранаты.
/// </summary>
public readonly struct GrenadeThrownContext(IGrenade grenade, CBaseCSGrenadeProjectile projectile) : IPostHookContext
{
    /// <summary>Брошенная граната.</summary>
    public IGrenade Grenade { get; } = grenade;

    /// <summary>Сущность снаряда.</summary>
    public CBaseCSGrenadeProjectile Projectile { get; } = projectile;
}
