using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events.Contexts.Grenades;
using CustomEquipment.Api.Events.Contexts.Items;

namespace CustomEquipment.Api.Events;

/// <summary>
/// События жизненного цикла пользовательских гранат.
/// </summary>
public interface ICustomEquipmentGrenadeEvents
{
    /// <summary>Вызывается перед выдачей пользовательской гранаты.</summary>
    IHookSubscription<GrenadeGivingContext> Giving { get; }

    /// <summary>Вызывается после успешной выдачи пользовательской гранаты.</summary>
    IHookSubscription<GrenadeGivenContext> Given { get; }

    /// <summary>Вызывается после появления снаряда, но до его регистрации как пользовательской гранаты.</summary>
    IHookSubscription<GrenadeThrowingContext> Throwing { get; }

    /// <summary>Вызывается после регистрации брошенной пользовательской гранаты.</summary>
    IHookSubscription<GrenadeThrownContext> Thrown { get; }

    /// <summary>Вызывается при ожидаемом отказе обработки броска.</summary>
    IHookSubscription<GrenadeThrowRejectedContext> ThrowRejected { get; }

    /// <summary>Вызывается перед пользовательской логикой детонации; её можно отменить.</summary>
    IHookSubscription<GrenadeDetonatingContext> Detonating { get; }

    /// <summary>Вызывается после пользовательской логики детонации.</summary>
    IHookSubscription<GrenadeDetonatedContext> Detonated { get; }

    /// <summary>Вызывается при ожидаемом отказе пользовательской детонации.</summary>
    IHookSubscription<GrenadeDetonationRejectedContext> DetonationRejected { get; }
}
