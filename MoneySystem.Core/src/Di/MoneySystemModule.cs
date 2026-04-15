using Common.Di;
using Microsoft.Extensions.DependencyInjection;
using MoneySystem.Core.Data.Configs;
using MoneySystem.Core.Services;
using SwiftlyS2.Shared;

namespace MoneySystem.Core.Di;

internal sealed class MoneySystemModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();
        
        service.AddSwiftly(core);

        BuildConfigs(service);
        BuildSingletons(service);
        
        return (service.BuildServiceProvider(), service);
    }

    private void BuildConfigs(ServiceCollection service)
    {
        AddConfig<MoneySystemConfig>(
            service: service,
            name: "money_system.json",
            section: "MoneySystemConfig"
        );
    }

    private void BuildSingletons(ServiceCollection service)
    {
        AddSingleton<IMoneyService, MoneyService>(service);
    }
}