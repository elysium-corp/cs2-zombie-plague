using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Shotguns;

namespace CS2ZombiePlague.Data.Weapons;

public sealed class WeaponFactory : IWeaponFactory
{
    public BaseWeapon Create<T>() where T : BaseWeapon
    {
        return typeof(T) switch
        {
            var t when t == typeof(Omega) => new Omega(),
            var t when t == typeof(X3) => new X3(),
            _ => throw new NotSupportedException("WeaponFactory: type T hasn't supported!")
        };
    }
}