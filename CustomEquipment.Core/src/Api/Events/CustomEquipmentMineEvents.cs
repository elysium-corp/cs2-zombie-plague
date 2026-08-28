using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Mines;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentMineEvents(IHookSubscriber hooks) : ICustomEquipmentMineEvents
{
    public IHookSubscription<MinePlacingContext> Placing { get; } =
        new HookEvent<MinePlacingContext>(hooks);

    public IHookSubscription<MinePlacedContext> Placed { get; } =
        new HookEvent<MinePlacedContext>(hooks);

    public IHookSubscription<MinePlacementRejectedContext> PlacementRejected { get; } =
        new HookEvent<MinePlacementRejectedContext>(hooks);
}
