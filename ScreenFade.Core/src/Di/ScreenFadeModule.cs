using Common.Di;
using Microsoft.Extensions.DependencyInjection;
using ScreenFade.Data.Configs;
using SwiftlyS2.Shared;

namespace ScreenFade.Di;

internal class ScreenFadeModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(core);
        
        AddConfig<ScreenFadeConfig>(
            service: service,
            name: "screen_fade.json",
            section: "ScreenFadeConfig"
        );

        return (service.BuildServiceProvider(), service);
    }
}