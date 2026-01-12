namespace CS2ZombiePlague.Data.Events;

public interface IEventSubscriber
{
    event EventDelegates.OnPlayerInfected? OnPlayerInfected;
}