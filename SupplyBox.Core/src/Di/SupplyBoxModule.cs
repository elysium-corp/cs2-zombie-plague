using Common.Di;
using Common.Hooks;
using Common.Hooks.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using SupplyBox.Api.Events;
using SupplyBox.Data.Configs;
using SupplyBox.Data.Entity;
using SupplyBox.Services;
using SwiftlyS2.Shared;

namespace SupplyBox.Di;

internal sealed class SupplyBoxModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(core);

        BuildConfigs(service);
        BuildSingletons(service);
        BuildTransients(service);

        return (service.BuildServiceProvider(), service);
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
        AddSingleton<HookService>(service);
        AddSingleton<IHookSubscriber>(service, provider => provider.GetRequiredService<HookService>());
        AddSingleton<IHookPublisher>(service, provider => provider.GetRequiredService<HookService>());
        AddSingleton<SupplyBoxPreEvents>(service);
        AddSingleton<SupplyBoxPostEvents>(service);
        AddSingleton<ISupplyBoxEvents, SupplyBoxEvents>(service);

        AddSingleton<SupplyBoxMapConfigService>(service);
        AddSingleton<SupplyBoxMenuService>(service);
        AddSingleton<SupplyBoxEditService>(service);
    }

    private void BuildTransients(ServiceCollection service)
    {
        AddTransient<SupplyBoxEntity>(service);
        AddTransient<SupplyBoxEntityTemplate>(service);
    }
}
