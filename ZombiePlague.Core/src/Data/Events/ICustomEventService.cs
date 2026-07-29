using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Data.Events;

internal interface ICustomEventService
{
    void FireFakeDeath(IPlayer attacker, IPlayer victim);
}
