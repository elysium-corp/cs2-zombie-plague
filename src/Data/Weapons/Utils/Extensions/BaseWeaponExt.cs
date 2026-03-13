using CS2ZombiePlague.Data.Weapons.Contracts;

namespace CS2ZombiePlague.Data.Weapons.Utils.Extensions;

public static class BaseWeaponExt
{
    extension(BaseWeapon? weapon)
    {
        public T? As<T>() where T : BaseWeapon
        {
            return weapon as T;
        }
    }
}