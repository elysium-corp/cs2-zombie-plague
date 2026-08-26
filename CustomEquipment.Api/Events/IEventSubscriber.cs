namespace CustomEquipment.Api.Events;

public interface IEventSubscriber
{
    event EventDelegates.OnItemBought? OnItemBought;
    
    event EventDelegates.OnItemGiven? OnItemGiven;
    
    event EventDelegates.OnGrenadeGiven? OnGrenadeGiven;
    
    event EventDelegates.OnWeaponGiven? OnWeaponGiven;
    
    event EventDelegates.OnGrenadeThrown? OnGrenadeThrown;

    event EventDelegates.OnGrenadeDetonated? OnGrenadeDetonated;
    
    event EventDelegates.OnMinePlaced? OnMinePlaced;
}