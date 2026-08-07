namespace ZombiePlague.Api.Events;

public interface IEventSubscriber
{
    event EventDelegates.OnPlayerInfected? OnPlayerInfected;
    event EventDelegates.OnPlayerDisinfected? OnPlayerDisinfected;
    
    // Round API
    event EventDelegates.OnRoundStarted? OnRoundStarted;
    event EventDelegates.OnRoundEnded? OnRoundEnded;
}