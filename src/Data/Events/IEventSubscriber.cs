namespace CS2ZombiePlague.Data.Events;

public interface IEventSubscriber
{
    event EventDelegates.OnPlayerInfectedBy? OnPlayerInfectedBy;
    event EventDelegates.OnPlayerInfected? OnPlayerInfected;
    event EventDelegates.OnEffectDestroyed? OnEffectDestroyed;
    event EventDelegates.OnGameRoundStarted? OnGameRoundStarted;
    event EventDelegates.OnSupplyBoxDropped? OnSupplyBoxDropped;
    event EventDelegates.OnSupplyBoxPickedUp? OnSupplyBoxPickedUp;
}