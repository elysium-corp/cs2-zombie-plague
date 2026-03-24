using SwiftlyS2.Shared.Players;
using ZPApi.Data;

namespace ZPApi.Events;

public interface IEventPublisher
{
    void OnPlayerInfectedBy(IPlayer infector, IPlayer victim);
    void OnPlayerInfected(IPlayer victim);
    void OnEffectDestroyed(IEffect effect);
    void OnGameRoundStarted(IRound round);
}