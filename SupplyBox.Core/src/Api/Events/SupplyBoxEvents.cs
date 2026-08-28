using Common.Hooks;
using Common.Hooks.Abstractions;
using SupplyBox.Api.Events.Contexts;

namespace SupplyBox.Api.Events;

internal sealed class SupplyBoxEvents(IHookSubscriber hooks) : ISupplyBoxEvents
{
    public IHookSubscription<SupplyBoxSpawningContext> Spawning { get; } =
        new HookEvent<SupplyBoxSpawningContext>(hooks);

    public IHookSubscription<SupplyBoxSpawnedContext> Spawned { get; } =
        new HookEvent<SupplyBoxSpawnedContext>(hooks);

    public IHookSubscription<SupplyBoxSpawnRejectedContext> SpawnRejected { get; } =
        new HookEvent<SupplyBoxSpawnRejectedContext>(hooks);

    public IHookSubscription<SupplyBoxLandedContext> Landed { get; } =
        new HookEvent<SupplyBoxLandedContext>(hooks);

    public IHookSubscription<SupplyBoxCollectingContext> Collecting { get; } =
        new HookEvent<SupplyBoxCollectingContext>(hooks);

    public IHookSubscription<SupplyBoxCollectedContext> Collected { get; } =
        new HookEvent<SupplyBoxCollectedContext>(hooks);

    public IHookSubscription<SupplyBoxCollectionRejectedContext> CollectionRejected { get; } =
        new HookEvent<SupplyBoxCollectionRejectedContext>(hooks);

    public IHookSubscription<SupplyBoxDestroyingContext> Destroying { get; } =
        new HookEvent<SupplyBoxDestroyingContext>(hooks);

    public IHookSubscription<SupplyBoxDestroyedContext> Destroyed { get; } =
        new HookEvent<SupplyBoxDestroyedContext>(hooks);
}
