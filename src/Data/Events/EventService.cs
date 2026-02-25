using CS2ZombiePlague.Data.Effects.Contracts;
using CS2ZombiePlague.Data.Rounds;
using CS2ZombiePlague.Data.SupplyBox;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Events;

public sealed class EventService : IEventSubscriber, IEventPublisher
{
    public event EventDelegates.OnPlayerInfectedBy? OnPlayerInfectedBy;
    public event EventDelegates.OnPlayerInfected? OnPlayerInfected;
    public event EventDelegates.OnEffectDestroyed? OnEffectDestroyed;
    public event EventDelegates.OnGameRoundStarted? OnGameRoundStarted;
    public event EventDelegates.OnSupplyBoxDropped? OnSupplyBoxDropped;
    public event EventDelegates.OnSupplyBoxPickedUp? OnSupplyBoxPickedUp;

    void IEventPublisher.OnPlayerInfectedBy(IPlayer infector, IPlayer victim)
    {
        var handlers = OnPlayerInfectedBy;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnPlayerInfectedBy)@delegate;
            try { handler(infector, victim); }
            catch (Exception ex)
            {
                // add custom logger
            }
        }
    }
    
    void IEventPublisher.OnPlayerInfected(IPlayer victim)
    {
        var handlers = OnPlayerInfected;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnPlayerInfected)@delegate;
            try { handler(victim); }
            catch (Exception ex)
            {
                // add custom logger
            }
        }
    }
    
    void IEventPublisher.OnEffectDestroyed(IEffect effect)
    {
        var handlers = OnEffectDestroyed;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnEffectDestroyed)@delegate;
            try { handler(effect); }
            catch (Exception ex)
            {
                // add custom logger
            }
        }
    }
    
    void IEventPublisher.OnGameRoundStarted(IRound round)
    {
        var handlers = OnGameRoundStarted;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnGameRoundStarted)@delegate;
            try { handler(round); }
            catch (Exception ex)
            {
                // add custom logger
            }
        }
    }

    void IEventPublisher.OnSupplyBoxDropped(SupplyBoxEntity supplyBox)
    {
        var handlers = OnSupplyBoxDropped;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnSupplyBoxDropped)@delegate;
            try { handler(supplyBox); }
            catch (Exception ex)
            {
                // add custom logger
            }
        }
    }

    void IEventPublisher.OnSupplyBoxPickedUp(IPlayer player, SupplyBoxEntity supplyBox)
    {
        var handlers = OnSupplyBoxPickedUp;
        
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnSupplyBoxPickedUp)@delegate;
            try { handler(player,  supplyBox); }
            catch (Exception ex)
            {
                // add custom logger
            }
        }
    }
}