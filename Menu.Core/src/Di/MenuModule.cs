using Common.Di;
using Menu.Api.Extensions;
using Menu.Core.Api;
using Menu.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace Menu.Core.Di;

internal class MenuModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();
        
        service.AddSwiftly(core);

        AddSingleton<MenuApi>(service);
        AddSingleton<MenuExtensionRegistry>(service);
        AddSingleton<IMenuExtensionRegistry>(
            service,
            provider => provider.GetRequiredService<MenuExtensionRegistry>()
        );
        AddSingleton<IMenuExtensionDispatcher>(
            service,
            provider => provider.GetRequiredService<MenuExtensionRegistry>()
        );

        return (service.BuildServiceProvider(), service);
    }
}