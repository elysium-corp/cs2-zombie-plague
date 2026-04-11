using CustomEquipment.Data.Equipments.Contracts;

namespace CustomEquipment.Utils;

internal static class WeaponExt
{
    extension<TWeapon>(IWeapon weapon) where TWeapon : class, IWeapon
    {
        internal TWeapon RequireAs()
        {
            return weapon as TWeapon ?? throw new InvalidCastException("IWeapon hasn't cast to BaseWeapon!");
        }

        internal TWeapon? As()
        {
            return weapon as TWeapon;
        }
    }
}