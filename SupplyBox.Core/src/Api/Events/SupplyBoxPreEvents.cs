using Common.Hooks;
using Common.Hooks.Abstractions;
using SupplyBox.Api.Events.Contexts;

namespace SupplyBox.Api.Events;

internal sealed class SupplyBoxPreEvents(IHookSubscriber hooks) : ISupplyBoxPreEvents
{
    private readonly HookEvent<SupplyBoxDropPreContext> _drop = new(hooks);
    private readonly HookEvent<SupplyBoxPickUpPreContext> _pickUp = new(hooks);

    public IHookSubscription<SupplyBoxDropPreContext> Drop => _drop;

    public event HookHandler<SupplyBoxDropPreContext> DropEvent
    {
        add => _drop.Event += value;
        remove => _drop.Event -= value;
    }

    public IHookSubscription<SupplyBoxPickUpPreContext> PickUp => _pickUp;

    public event HookHandler<SupplyBoxPickUpPreContext> PickUpEvent
    {
        add => _pickUp.Event += value;
        remove => _pickUp.Event -= value;
    }
}
