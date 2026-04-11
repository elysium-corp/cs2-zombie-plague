namespace ZombiePlague.Api.Events;

public interface IEventSubscriber
{
    event EventDelegates.OnPlayerInfectedBy? OnPlayerInfectedBy;
    event EventDelegates.OnPlayerInfected? OnPlayerInfected;
    event EventDelegates.OnGameRoundStarted? OnGameRoundStarted;
}