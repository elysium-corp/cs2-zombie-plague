using ZPCore.Data.Weapons.Contracts;

namespace ZPCore.Data.Weapons;

internal interface IWeaponFactory
{
    public BaseWeapon Create<T>() where T : BaseWeapon;
    
    public BaseWeapon Create(string internalName);
}