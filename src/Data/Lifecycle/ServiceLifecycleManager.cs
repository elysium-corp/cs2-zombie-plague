using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Di;
using CS2ZombiePlague.Service.Contracts;

namespace CS2ZombiePlague.Data.Lifecycle;

public sealed class ServiceLifecycleManager : ILifecycle
{
    public void Dispose()
    {
        var particleService = DependencyManager.GetService<IWeaponParticleService>();
        var weaponService = DependencyManager.GetService<IWeaponService>();
        var humanManager = DependencyManager.GetService<HumanManager>();
        
        particleService.Dispose();
        weaponService.Dispose();
        humanManager.Dispose();
    }
}