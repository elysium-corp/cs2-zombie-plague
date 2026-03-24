using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Enums;

namespace ZPCore.Data.Weapons;

internal interface IWeaponRegistrator
{
    void Registration();

    List<IWeaponPurchasable>? GetWeaponsByType(WeaponType type);

    public List<BaseWeapon> GetAllWeapons();
}