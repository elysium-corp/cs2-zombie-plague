using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Grenades;
using ZPCore.Data.Weapons.Utils.Extensions;

namespace ZPCore.Data.Weapons;

internal class GrenadeFactory(IWeaponRegistrator weaponRegistrator) : IGrenadeFactory
{
    public BaseGrenade Create<T>() where T : BaseGrenade
    {
        return typeof(T) switch
        {
            var t when t == typeof(BarrierNade) => new BarrierNade(),
            _ => throw new NotSupportedException("GrenadeFactory: type T hasn't supported!")
        };
    }

    public BaseGrenade Create(string internalName)
    {
        return weaponRegistrator.GetAllWeapons().Find(wp => wp.InternalName == internalName).As<BaseGrenade>() ??
               throw new NotSupportedException("GrenadeFactory: internalName hasn't supported!");
    }
}