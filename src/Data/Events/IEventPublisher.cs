using CS2ZombiePlague.Data.ZClasses;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Events;

public interface IEventPublisher
{
    void OnPlayerInfected(IPlayer player, IZClass zClass);
}