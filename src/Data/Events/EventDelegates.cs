using CS2ZombiePlague.Data.Rounds;
using CS2ZombiePlague.Data.SupplyBox;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Events;

public class EventDelegates
{
    public delegate void OnPlayerInfectedBy(IPlayer infector, IPlayer victim);  
    public delegate void OnPlayerInfected(IPlayer victim);  
    public delegate void OnGameRoundStarted(IRound round);
    public delegate void OnSupplyBoxDropped(SupplyBoxEntity supplyBox);
    public delegate void OnSupplyBoxPickedUp (IPlayer player, SupplyBoxEntity supplyBox);
    
}