using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Enums;

namespace CS2ZombiePlague.Data.Weapons;

public interface IWeaponRegistrator
{
    void Registration();

    List<IWeaponPurchasable>? GetWeaponsByType(WeaponType type);

    public List<BaseWeapon> GetAllWeapons();
}