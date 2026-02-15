using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Events;

public sealed class EventService : IEventSubscriber, IEventPublisher
{
    public event EventDelegates.OnPlayerInfectedBy? OnPlayerInfectedBy;
    public event EventDelegates.OnPlayerInfected? OnPlayerInfected;
    public event EventDelegates.OnWeaponDrop? OnWeaponDrop;

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
    
    void IEventPublisher.OnWeaponDrop(IPlayer player, CCSWeaponBase weapon)
    {
        var handlers = OnWeaponDrop;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnWeaponDrop)@delegate;
            try { handler(player, weapon); }
            catch (Exception ex)
            {
                // add custom logger
            }
        }
    }
}