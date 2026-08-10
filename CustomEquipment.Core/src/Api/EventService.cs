using CustomEquipment.Data.Equipments.Contracts;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api;

public class EventService : IEventSubscriber, IEventPublisher
{
    private readonly ISwiftlyCore? _core;

    public EventService()
    {
    }

    public EventService(ISwiftlyCore core)
    {
        _core = core;
    }

    public event EventDelegates.OnItemGiven? OnItemGiven;
    public event EventDelegates.OnGrenadeGiven? OnGrenadeGiven;
    public event EventDelegates.OnWeaponGiven? OnWeaponGiven;
    public event EventDelegates.OnGrenadeThrown? OnGrenadeThrown;
    public event EventDelegates.OnGrenadeDetonated? OnGrenadeDetonated;

    void IEventPublisher.OnItemGiven(IPlayer player, IItem item)
    {
        var handlers = OnItemGiven;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnItemGiven)@delegate;
            InvokeSafely(nameof(OnItemGiven), () => handler(player, item));
        }
    }
    
    void IEventPublisher.OnGrenadeGiven(IPlayer player, IGrenade grenade)
    {
        var handlers = OnGrenadeGiven;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnGrenadeGiven)@delegate;
            InvokeSafely(nameof(OnGrenadeGiven), () => handler(player, grenade));
        }
    }
    
    void IEventPublisher.OnWeaponGiven(IPlayer player, IWeapon weapon)
    {
        var handlers = OnWeaponGiven;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnWeaponGiven)@delegate;
            InvokeSafely(nameof(OnWeaponGiven), () => handler(player, weapon));
        }
    }
    
    void IEventPublisher.OnGrenadeThrown(IGrenade grenade, CBaseCSGrenadeProjectile projectile)
    {
        var handlers = OnGrenadeThrown;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnGrenadeThrown)@delegate;
            InvokeSafely(nameof(OnGrenadeThrown), () => handler(grenade, projectile));
        }
    }
    
    void IEventPublisher.OnGrenadeDetonated(IGrenade grenade, CBaseCSGrenadeProjectile projectile, Vector position)
    {
        var handlers = OnGrenadeDetonated;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnGrenadeDetonated)@delegate;
            InvokeSafely(nameof(OnGrenadeDetonated), () => handler(grenade, projectile, position));
        }
    }

    private void InvokeSafely(string eventName, Action callback)
    {
        try
        {
            callback();
        }
        catch (Exception exception)
        {
            _core?.Logger.LogError(
                exception,
                "CustomEquipment event handler failed for {EventName}.",
                eventName
            );
        }
    }
}
