using CS2ZombiePlague.Data.ZClasses;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Events;

public class EventDelegates
{
    public delegate void OnPlayerInfected(IPlayer player, IZClass zClass);
}