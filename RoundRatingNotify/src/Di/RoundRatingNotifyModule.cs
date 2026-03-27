using Common.Di;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace RoundRatingNotify.Di;

internal class RoundRatingNotifyModule(ISwiftlyCore core) : BaseModule(core)
{
    public override ServiceProvider GetProvider()
    {
        var service = new ServiceCollection();

        service.AddSwiftly(core);

        return service.BuildServiceProvider();
    }
}