using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Data.Events;

public interface ICustomEventService
{
    void FireFakeDeath(IPlayer attacker, IPlayer? victim);
}