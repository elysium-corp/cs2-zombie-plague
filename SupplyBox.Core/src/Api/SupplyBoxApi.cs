using SupplyBox.Api.Events;

namespace SupplyBox.Api;

public sealed class SupplyBoxApi(ISupplyBoxEvents events) : ISupplyBoxApi
{
    public ISupplyBoxEvents Events => events;
}
