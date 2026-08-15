using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Data.Events;

public interface ICustomEventService
{
    void ShowInfection(IPlayer? attacker, IPlayer? victim);
}