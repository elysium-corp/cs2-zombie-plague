using CS2ZombiePlague.Data.Weapons.Contracts;

namespace CS2ZombiePlague.Data.Weapons;

public interface IWeaponFactory
{
    public BaseWeapon Create<T>() where T : BaseWeapon;
}