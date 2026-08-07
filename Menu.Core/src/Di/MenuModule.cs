using Common.Di;
using Menu.Api.Data.Factory;
using Menu.Api.Events;
using Menu.Core.Factory;
using Menu.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace Menu.Core.Di;

internal class MenuModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();
        
        service.AddSwiftly(core);
        
        AddSingleton<IMenuFactory, MenuFactory>(service);
        AddSingleton<IMenuService, MenuService>(service);
        AddSingleton<EventService>(service);
        AddSingleton<IEventSubscriber>(service, s => s.GetRequiredService<EventService>());
        AddSingleton<IEventPublisher>(service, s => s.GetRequiredService<EventService>());
        
        return (service.BuildServiceProvider(), service);
    }
}