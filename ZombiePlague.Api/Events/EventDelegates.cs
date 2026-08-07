using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data;

namespace ZombiePlague.Api.Events;

public class EventDelegates
{ 
    public delegate void OnPlayerInfected(IPlayer infected, IPlayer? infector);  
    public delegate void OnPlayerDisinfected(IPlayer disinfected);
    
    // Round API
    public delegate void OnRoundStarted(IRound round);
    public delegate void OnRoundEnded(IRound round);
}