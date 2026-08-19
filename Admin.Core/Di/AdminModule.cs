using Admin.Core.Di.Store;
using Admin.Core.Registry;
using Admin.Core.Services;
using Common.Di;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace Admin.Core.Di;

internal sealed class AdminModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();
        
        service.AddSwiftly(Core);
        
        AddSingleton<IPrivilegeRegistry, PrivilegeRegistry>(service);
        AddSingleton<IPlayerPrivilegeStore, PlayerPrivilegeStore>(service);
        AddSingleton<IPrivilegeService, PrivilegeService>(service);

        return (service.BuildServiceProvider(), service);
    }
}