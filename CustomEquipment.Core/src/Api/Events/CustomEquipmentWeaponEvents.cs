using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Items;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentWeaponEvents(IHookSubscriber hooks) : ICustomEquipmentWeaponEvents
{
    public IHookSubscription<WeaponGivingContext> Giving { get; } =
        new HookEvent<WeaponGivingContext>(hooks);

    public IHookSubscription<WeaponGivenContext> Given { get; } =
        new HookEvent<WeaponGivenContext>(hooks);

    public IHookSubscription<WeaponDamageModifyingContext> DamageModifying { get; } =
        new HookEvent<WeaponDamageModifyingContext>(hooks);

    public IHookSubscription<WeaponDamageModifiedContext> DamageModified { get; } =
        new HookEvent<WeaponDamageModifiedContext>(hooks);

    public IHookSubscription<WeaponImpactProcessingContext> ImpactProcessing { get; } =
        new HookEvent<WeaponImpactProcessingContext>(hooks);

    public IHookSubscription<WeaponImpactProcessedContext> ImpactProcessed { get; } =
        new HookEvent<WeaponImpactProcessedContext>(hooks);

    public IHookSubscription<WeaponAmmoPurchasingContext> AmmoPurchasing { get; } =
        new HookEvent<WeaponAmmoPurchasingContext>(hooks);

    public IHookSubscription<WeaponAmmoPurchasedContext> AmmoPurchased { get; } =
        new HookEvent<WeaponAmmoPurchasedContext>(hooks);

    public IHookSubscription<WeaponAmmoPurchaseRejectedContext> AmmoPurchaseRejected { get; } =
        new HookEvent<WeaponAmmoPurchaseRejectedContext>(hooks);
}
