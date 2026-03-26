using Common.Di;
using Microsoft.Extensions.DependencyInjection;
using SupplyBox.Data.Configs;
using SupplyBox.Data.Entity;
using SupplyBox.Events;
using SupplyBox.Services;
using SwiftlyS2.Shared;

namespace SupplyBox.Di;

internal sealed class SupplyBoxModule(ISwiftlyCore core) : BaseModule(core)
{
    public override ServiceProvider GetProvider()
    {
        var service = new ServiceCollection();
        
        service.AddSwiftly(core);

        BuildConfigs(service);
        BuildSingletons(service);
        BuildTransients(service);
            
        return service.BuildServiceProvider();
    }

    private void BuildConfigs(ServiceCollection service)
    {
        AddConfig<SupplyBoxConfig>(
            service: service,
            name: "supply_box.json",
            section: "SupplyBox"
        );
    }

    private void BuildSingletons(ServiceCollection service)
    {
        AddSingleton<SupplyBoxMapConfigService>(service);
        AddSingleton<SupplyBoxMenuService>(service);
        AddSingleton<SupplyBoxEditService>(service);
        AddSingleton<EventService>(service);
        AddSingleton<IEventSubscriber>(service, s => s.GetRequiredService<EventService>());
        AddSingleton<IEventPublisher>(service, s => s.GetRequiredService<EventService>());
    }

    private void BuildTransients(ServiceCollection service)
    {
        AddTransient<SupplyBoxEntity>(service);
        AddTransient<SupplyBoxEntityTemplate>(service);
    }
}