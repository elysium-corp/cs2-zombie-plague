using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events.Contexts.Items;

namespace CustomEquipment.Api.Events;

/// <summary>
/// События выдачи пользовательского оружия.
/// </summary>
public interface ICustomEquipmentWeaponEvents
{
    /// <summary>Вызывается перед выдачей пользовательского оружия.</summary>
    IHookSubscription<WeaponGivingContext> Giving { get; }

    /// <summary>Вызывается после успешной выдачи пользовательского оружия.</summary>
    IHookSubscription<WeaponGivenContext> Given { get; }

    /// <summary>Вызывается на пути получения урона перед применением множителя пользовательского оружия.</summary>
    IHookSubscription<WeaponDamageModifyingContext> DamageModifying { get; }

    /// <summary>Вызывается после применения множителя урона пользовательского оружия.</summary>
    IHookSubscription<WeaponDamageModifiedContext> DamageModified { get; }

    /// <summary>Вызывается перед созданием эффектов попадания пули.</summary>
    IHookSubscription<WeaponImpactProcessingContext> ImpactProcessing { get; }

    /// <summary>Вызывается после создания эффектов попадания пули.</summary>
    IHookSubscription<WeaponImpactProcessedContext> ImpactProcessed { get; }

    /// <summary>Вызывается перед списанием денег за боеприпасы.</summary>
    IHookSubscription<WeaponAmmoPurchasingContext> AmmoPurchasing { get; }

    /// <summary>Вызывается после списания денег и обновления запаса боеприпасов.</summary>
    IHookSubscription<WeaponAmmoPurchasedContext> AmmoPurchased { get; }

    /// <summary>Вызывается при ожидаемом отказе покупки боеприпасов.</summary>
    IHookSubscription<WeaponAmmoPurchaseRejectedContext> AmmoPurchaseRejected { get; }
}
