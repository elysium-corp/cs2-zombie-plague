using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Grenades;
using CS2ZombiePlague.Data.Weapons.Utils.Extensions;

namespace CS2ZombiePlague.Data.Weapons;

public class GrenadeFactory(IWeaponRegistrator weaponRegistrator) : IGrenadeFactory
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