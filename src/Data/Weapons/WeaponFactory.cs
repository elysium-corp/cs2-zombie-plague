using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Guns;

namespace CS2ZombiePlague.Data.Weapons;

public sealed class WeaponFactory(IWeaponRegistrator weaponRegistrator) : IWeaponFactory
{
    public BaseWeapon Create<T>() where T : BaseWeapon
    {
        return typeof(T) switch
        {
            var t when t == typeof(Omega) => new Omega(),
            var t when t == typeof(X3) => new X3(),
            var t when t == typeof(Blackline) => new Blackline(),
            var t when t == typeof(Elite) => new Elite(),
            var t when t == typeof(Frostbyte) => new Frostbyte(),
            var t when t == typeof(ReactorLeak) => new ReactorLeak(),
            _ => throw new NotSupportedException("WeaponFactory: type T hasn't supported!")
        };
    }

    public BaseWeapon Create(string internalName)
    {
        return weaponRegistrator.GetAllWeapons().Find(wp => wp.InternalName == internalName) ??
               throw new NotSupportedException("WeaponFactory: internalName hasn't supported!");
    }
}