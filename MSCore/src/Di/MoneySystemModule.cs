using Common.Di;
using Microsoft.Extensions.DependencyInjection;
using MoneySystem.Data.Configs;
using MoneySystem.Services;
using SwiftlyS2.Shared;

namespace MoneySystem.Di;

internal sealed class MoneySystemModule(ISwiftlyCore core) : BaseModule(core)
{
    private readonly ISwiftlyCore _core = core;

    public override ServiceProvider GetProvider()
    {
        var service = new ServiceCollection();
        
        service.AddSwiftly(_core);

        BuildConfigs(service);
        BuildSingletons(service);
        
        return service.BuildServiceProvider();
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