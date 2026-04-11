using Common.Di;
using DamageNotify.Core.Data.Configs;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace DamageNotify.Core.Di;

internal sealed class DamageNotifyModule(ISwiftlyCore core) : BaseModule(core)
{
    private readonly ISwiftlyCore _core = core;

    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(_core);
        
        AddConfig<DamageNotifyConfig>(
            service: service,
            name: "damage_notify.json",
            section: "DamageNotifyConfig"
        );
        
        return (service.BuildServiceProvider(), service);
    }
}