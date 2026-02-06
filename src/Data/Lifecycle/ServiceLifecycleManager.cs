using CS2ZombiePlague.Di;
using CS2ZombiePlague.Service.Contracts;

namespace CS2ZombiePlague.Data.Lifecycle;

public sealed class ServiceLifecycleManager : ILifecycle
{
    public void Dispose()
    {
        var particleService = DependencyManager.GetService<IWeaponParticleService>();
        var weaponService = DependencyManager.GetService<IWeaponService>();
        
        particleService.Dispose();
        weaponService.Dispose();
    }
}