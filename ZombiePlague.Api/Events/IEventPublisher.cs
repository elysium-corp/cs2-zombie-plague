using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data;

namespace ZombiePlague.Api.Events;

public interface IEventPublisher
{
    void OnPlayerInfected(IPlayer infected, IPlayer? infector = null);
    void OnPlayerDisinfected(IPlayer disinfected);
    
    
    // Round API 
    void OnRoundStarted(IRound round);
    void OnRoundEnded(IRound round); 
}