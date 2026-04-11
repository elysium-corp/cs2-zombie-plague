using Common.Di;
using InfoNotify.Core.Data.Configs;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace InfoNotify.Core.Di;

internal sealed class InfoNotifyModule(ISwiftlyCore core) : BaseModule(core)
{
    private readonly ISwiftlyCore _core = core;

    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();
        
        service.AddSwiftly(_core);

        BuildConfigs(service);
        
        return (service.BuildServiceProvider(), service);
    }

    private void BuildConfigs(ServiceCollection service)
    {
        AddConfig<InfoNotifyConfig>(
            service: service,
            name: "info_notify.json",
            section: "InfoNotifyConfig"
        );
    }
}