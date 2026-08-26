using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api;

internal class EventService : IEventSubscriber, IEventPublisher
{
    public event EventDelegates.OnItemBought? OnItemBought;
    public event EventDelegates.OnItemGiven? OnItemGiven;
    public event EventDelegates.OnGrenadeGiven? OnGrenadeGiven;
    public event EventDelegates.OnWeaponGiven? OnWeaponGiven;
    public event EventDelegates.OnGrenadeThrown? OnGrenadeThrown;
    public event EventDelegates.OnGrenadeDetonated? OnGrenadeDetonated;
    public event EventDelegates.OnMinePlaced? OnMinePlaced;

    void IEventPublisher.OnItemBought(IPlayer player, IShopItem item)
    {
        var handlers = OnItemBought;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnItemBought)@delegate;
            handler(player, item);
        }
    }

    void IEventPublisher.OnItemGiven(IPlayer player, IItem item)
    {
        var handlers = OnItemGiven;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnItemGiven)@delegate;
            handler(player, item);
        }
    }
    
    void IEventPublisher.OnGrenadeGiven(IPlayer player, IGrenade grenade)
    {
        var handlers = OnGrenadeGiven;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnGrenadeGiven)@delegate;
            handler(player, grenade);
        }
    }
    
    void IEventPublisher.OnWeaponGiven(IPlayer player, IWeapon weapon)
    {
        var handlers = OnWeaponGiven;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnWeaponGiven)@delegate;
            handler(player, weapon);
        }
    }
    
    void IEventPublisher.OnGrenadeThrown(IGrenade grenade, CBaseCSGrenadeProjectile projectile)
    {
        var handlers = OnGrenadeThrown;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnGrenadeThrown)@delegate;
            handler(grenade, projectile);
        }
    }
    
    void IEventPublisher.OnGrenadeDetonated(IGrenade grenade, CBaseCSGrenadeProjectile projectile, Vector position)
    {
        var handlers = OnGrenadeDetonated;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnGrenadeDetonated)@delegate;
            handler(grenade, projectile, position);
        }
    }

    void IEventPublisher.OnMinePlaced(IPlayer player, LaserMineEntityBase mine)
    {
        var handlers = OnMinePlaced;
        if (handlers == null) return;
        
        foreach (var @delegate in handlers.GetInvocationList())
        {
            var handler = (EventDelegates.OnMinePlaced)@delegate;
            handler(player, mine);
        }
    }
}