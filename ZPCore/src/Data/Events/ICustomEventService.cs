using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Events;

public interface ICustomEventService
{
    void FireFakeDeath(IPlayer attacker, IPlayer? victim);
}