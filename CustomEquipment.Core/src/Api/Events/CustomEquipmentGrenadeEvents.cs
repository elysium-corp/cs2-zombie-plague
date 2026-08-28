using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Grenades;
using CustomEquipment.Api.Events.Contexts.Items;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentGrenadeEvents(IHookSubscriber hooks) : ICustomEquipmentGrenadeEvents
{
    public IHookSubscription<GrenadeGivingContext> Giving { get; } =
        new HookEvent<GrenadeGivingContext>(hooks);

    public IHookSubscription<GrenadeGivenContext> Given { get; } =
        new HookEvent<GrenadeGivenContext>(hooks);

    public IHookSubscription<GrenadeThrowingContext> Throwing { get; } =
        new HookEvent<GrenadeThrowingContext>(hooks);

    public IHookSubscription<GrenadeThrownContext> Thrown { get; } =
        new HookEvent<GrenadeThrownContext>(hooks);

    public IHookSubscription<GrenadeThrowRejectedContext> ThrowRejected { get; } =
        new HookEvent<GrenadeThrowRejectedContext>(hooks);

    public IHookSubscription<GrenadeDetonatingContext> Detonating { get; } =
        new HookEvent<GrenadeDetonatingContext>(hooks);

    public IHookSubscription<GrenadeDetonatedContext> Detonated { get; } =
        new HookEvent<GrenadeDetonatedContext>(hooks);

    public IHookSubscription<GrenadeDetonationRejectedContext> DetonationRejected { get; } =
        new HookEvent<GrenadeDetonationRejectedContext>(hooks);
}
