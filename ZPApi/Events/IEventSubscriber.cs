namespace ZPApi.Events;

public interface IEventSubscriber
{
    event EventDelegates.OnPlayerInfectedBy? OnPlayerInfectedBy;
    event EventDelegates.OnPlayerInfected? OnPlayerInfected;
    event EventDelegates.OnEffectDestroyed? OnEffectDestroyed;
    event EventDelegates.OnGameRoundStarted? OnGameRoundStarted;
}