using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Events;

public class EventDelegates
{
    public delegate void OnPlayerInfectedBy(IPlayer infector, IPlayer victim);  
}