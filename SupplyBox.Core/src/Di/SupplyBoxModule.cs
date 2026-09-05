using Common.Di;
using Common.Hooks;
using Common.Hooks.Abstractions;
using Common.Di.Utils;
using Localization.Api;
using Economy.Api;
using CustomEquipment.Api;
using Microsoft.Extensions.Options;
using SupplyBox.Database;
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

        service.AddSwiftly(Core);
        service.AddSharedInterface<ILocalizationApi>();
        service.AddSharedInterface<IEconomyApi>();
        service.AddSharedInterface<ICustomEquipmentApi>();

        BuildConfigs(service);
        BuildSingletons(service);
        BuildTransients(service);

        return (service.BuildServiceProvider(), service);
    }

    private void BuildConfigs(ServiceCollection service)
    {
        service.AddSingleton<IOptions<SupplyBoxConfig>>(provider => provider.GetRequiredService<SupplyBoxMapConfigService>());
    }

    private void BuildSingletons(ServiceCollection service)
    {
        AddSingleton<HookService>(service);
        AddSingleton<IHookSubscriber>(service, provider => provider.GetRequiredService<HookService>());
        AddSingleton<IHookPublisher>(service, provider => provider.GetRequiredService<HookService>());
        AddSingleton<ISupplyBoxEvents, SupplyBoxEvents>(service);

        AddSingleton<SupplyBoxRepository>(service);
        AddSingleton<SupplyBoxMapConfigService>(service);
        AddSingleton<SupplyBoxRewardService>(service);
        AddSingleton<SupplyBoxMenuService>(service);
        AddSingleton<SupplyBoxEditService>(service);
    }

    private void BuildTransients(ServiceCollection service)
    {
        AddTransient<SupplyBoxEntity>(service);
        AddTransient<SupplyBoxEntityTemplate>(service);
    }
}
