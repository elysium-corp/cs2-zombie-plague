using CS2ZombiePlague.Data.Weapons.Enums;

namespace CS2ZombiePlague.Data.Weapons.Contracts;

public interface IWeaponPurchasable : IWeapon
{ 
    int Coast { get; }
    
    WeaponType WeaponType { get; }
}