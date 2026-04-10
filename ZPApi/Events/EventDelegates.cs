using SwiftlyS2.Shared.Players;
using ZPApi.Data;

namespace ZPApi.Events;

public class EventDelegates
{
    public delegate void OnPlayerInfectedBy(IPlayer infector, IPlayer victim);  
    public delegate void OnPlayerInfected(IPlayer victim);  
    public delegate void OnGameRoundStarted(IRound round);
}