using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Events;

public interface IEventPublisher
{
    void OnPlayerInfectedBy(IPlayer infector, IPlayer victim);
}