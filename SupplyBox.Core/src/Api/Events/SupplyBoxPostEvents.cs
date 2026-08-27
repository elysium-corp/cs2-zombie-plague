using Common.Hooks;
using Common.Hooks.Abstractions;
using SupplyBox.Api.Events.Contexts;

namespace SupplyBox.Api.Events;

internal sealed class SupplyBoxPostEvents(IHookSubscriber hooks) : ISupplyBoxPostEvents
{
    private readonly HookEvent<SupplyBoxDropPostContext> _drop = new(hooks);
    private readonly HookEvent<SupplyBoxPickUpPostContext> _pickUp = new(hooks);

    public IHookSubscription<SupplyBoxDropPostContext> Drop => _drop;

    public event HookHandler<SupplyBoxDropPostContext> DropEvent
    {
        add => _drop.Event += value;
        remove => _drop.Event -= value;
    }

    public IHookSubscription<SupplyBoxPickUpPostContext> PickUp => _pickUp;

    public event HookHandler<SupplyBoxPickUpPostContext> PickUpEvent
    {
        add => _pickUp.Event += value;
        remove => _pickUp.Event -= value;
    }
}
