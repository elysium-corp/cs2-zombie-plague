using CS2ZombiePlague.Data.Effects.Contracts;
using CS2ZombiePlague.Data.Rounds;
using CS2ZombiePlague.Data.SupplyBox;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Events;

public interface IEventPublisher
{
    void OnPlayerInfectedBy(IPlayer infector, IPlayer victim);
    void OnPlayerInfected(IPlayer victim);
    void OnEffectDestroyed(IEffect effect);
    void OnGameRoundStarted(IRound round);
    void OnSupplyBoxDropped(SupplyBoxEntity supplyBox);
    void OnSupplyBoxPickedUp (IPlayer player, SupplyBoxEntity supplyBox);
}