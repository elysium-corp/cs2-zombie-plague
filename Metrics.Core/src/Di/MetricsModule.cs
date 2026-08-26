using Common.Di;
using Metrics.Api;
using Metrics.Core.Config;
using Metrics.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;

namespace Metrics.Core.Di;

internal sealed class MetricsModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var services = new ServiceCollection();

        AddConfig<MetricsConfig>(
            service: services,
            name: "metrics.json",
            section: "MetricsConfig",
            reloadOnChange: false
        );

        services.AddSwiftly(core);

        AddSingleton<MetricsHttpClient>(services);
        AddSingleton<MetricsSpool>(services);
        AddSingleton<MetricsService>(services);
        AddSingleton<IMetricsService>(
            services,
            provider => provider.GetRequiredService<MetricsService>()
        );

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        return (provider, services);
    }
}
