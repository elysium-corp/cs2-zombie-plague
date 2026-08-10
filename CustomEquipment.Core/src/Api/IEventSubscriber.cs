namespace CustomEquipment.Api;

public interface IEventSubscriber
{
    event EventDelegates.OnItemGiven? OnItemGiven;
    
    event EventDelegates.OnGrenadeGiven? OnGrenadeGiven;
    
    event EventDelegates.OnWeaponGiven? OnWeaponGiven;
    
    event EventDelegates.OnGrenadeThrown? OnGrenadeThrown;

    event EventDelegates.OnGrenadeDetonated? OnGrenadeDetonated;
}
