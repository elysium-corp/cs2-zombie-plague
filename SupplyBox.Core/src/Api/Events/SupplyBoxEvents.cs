using SupplyBox.Api.Events;

namespace SupplyBox.Api.Events;

internal sealed class SupplyBoxEvents(
    SupplyBoxPreEvents pre,
    SupplyBoxPostEvents post) : ISupplyBoxEvents
{
    public ISupplyBoxPreEvents Pre => pre;

    public ISupplyBoxPostEvents Post => post;
}
