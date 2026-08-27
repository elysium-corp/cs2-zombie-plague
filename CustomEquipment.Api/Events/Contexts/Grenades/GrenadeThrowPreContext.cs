using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api.Events.Contexts.Grenades;

/// <summary>
/// Контекст перед обработкой броска гранаты.
/// </summary>
public struct GrenadeThrowPreContext(IGrenade grenade, CBaseCSGrenadeProjectile projectile) : IPreHookContext
{
    /// <summary>Брошенная граната.</summary>
    public IGrenade Grenade { get; set; } = grenade;

    /// <summary>Сущность снаряда.</summary>
    public CBaseCSGrenadeProjectile Projectile { get; set; } = projectile;


    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
