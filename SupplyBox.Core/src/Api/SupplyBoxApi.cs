using SupplyBox.Events;

namespace SupplyBox.Api;

public sealed class SupplyBoxApi(IEventSubscriber eventSubscriber) : ISupplyBoxApi
{
    public IEventSubscriber EventSubscriber => eventSubscriber;
}