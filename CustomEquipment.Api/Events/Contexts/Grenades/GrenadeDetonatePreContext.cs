using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api.Events.Contexts.Grenades;

/// <summary>
/// Контекст перед пользовательской детонацией гранаты.
/// </summary>
public struct GrenadeDetonatePreContext(IGrenade grenade, CBaseCSGrenadeProjectile projectile, Vector position) : IPreHookContext
{
    /// <summary>Детонирующая граната.</summary>
    public IGrenade Grenade { get; set; } = grenade;

    /// <summary>Сущность снаряда.</summary>
    public CBaseCSGrenadeProjectile Projectile { get; set; } = projectile;

    /// <summary>Позиция детонации.</summary>
    public Vector Position { get; set; } = position;


    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
