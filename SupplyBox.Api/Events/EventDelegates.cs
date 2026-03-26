using SupplyBox.Data;
using SwiftlyS2.Shared.Players;

namespace SupplyBox.Events;

public class EventDelegates
{
    public delegate void OnSupplyBoxDropped(ISupplyBoxEntity supplyBox);
    public delegate void OnSupplyBoxPickedUp(IPlayer player, ISupplyBoxEntity supplyBox);
}