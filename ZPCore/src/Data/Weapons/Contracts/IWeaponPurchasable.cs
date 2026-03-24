using ZPCore.Data.Weapons.Enums;

namespace ZPCore.Data.Weapons.Contracts;

internal interface IWeaponPurchasable : IWeapon
{ 
    int Coast { get; }
    
    WeaponType WeaponType { get; }
}