using SupplyBox.Data;
using SwiftlyS2.Shared.Players;

namespace SupplyBox.Events;

public interface IEventPublisher
{
    void OnSupplyBoxDropped(ISupplyBoxEntity supplyBox);
    void OnSupplyBoxPickedUp(IPlayer player, ISupplyBoxEntity supplyBox);
}