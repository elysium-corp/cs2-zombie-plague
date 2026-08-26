using Common.Di;
using Metrics.Api;
using Metrics.Core.Di;
using Metrics.Core.Services;
using SwiftlyS2.Shared;

namespace Metrics.Core;

[PluginMetadata(
    Id = "Metrics.Core",
    Version = "0.1.0",
    Name = "Elysium Metrics",
    Author = "Elysium",
    Description = "Queues and delivers game analytics events to Elysium Metrics"
)]
internal sealed partial class Metrics(ISwiftlyCore core) : Plugin<MetricsModule>(core)
{
    private readonly Lazy<MetricsService> _metricsService = GetRequiredServiceLazy<MetricsService>();

    protected override void OnStart()
    {
        _metricsService.Value.Start();
    }

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IMetricsService, MetricsService>(
            IMetricsService.SharedApiKey,
            _metricsService.Value
        );
    }

    protected override void OnUnload()
    {
        _metricsService.Value.StopAndWait();
    }
}
