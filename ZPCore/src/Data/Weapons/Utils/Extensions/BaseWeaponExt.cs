using ZPCore.Data.Weapons.Contracts;

namespace ZPCore.Data.Weapons.Utils.Extensions;

internal static class BaseWeaponExt
{
    extension(BaseWeapon? weapon)
    {
        public T? As<T>() where T : BaseWeapon
        {
            return weapon as T;
        }
    }
}