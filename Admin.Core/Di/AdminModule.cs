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

        return (service.BuildServiceProvider(), service);
    }
}