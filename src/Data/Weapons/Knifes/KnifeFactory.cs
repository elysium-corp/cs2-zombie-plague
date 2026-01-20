using CS2ZombiePlague.Di;

namespace CS2ZombiePlague.Data.Weapons.Knifes;

public class KnifeFactory() : IKnifeFactory
{
    public IKnife Create<T>() where T : IKnife
    {
        return typeof(T) switch
        {
            var t when t == typeof(KnockbackKnifeWeapon) => DependencyManager.GetService<KnockbackKnifeWeapon>(),
            var t when t == typeof(SpeedKnifeWeapon) => DependencyManager.GetService<SpeedKnifeWeapon>(),
            var t when t == typeof(GravityKnifeWeapon) => DependencyManager.GetService<GravityKnifeWeapon>(),
            var t when t == typeof(VipKnifeWeapon) => DependencyManager.GetService<VipKnifeWeapon>(),
            _ => throw new NotSupportedException("KnifeFactory: type T hasn't supported!")
        };
    }
}