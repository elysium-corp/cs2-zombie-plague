using ZPCore.Data.Managers;
using ZPCore.Di;
using ZPCore.Service.Contracts;

namespace ZPCore.Data.Lifecycle;

internal sealed class ServiceLifecycleManager : ILifecycle
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