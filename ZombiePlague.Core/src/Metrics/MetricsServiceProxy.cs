using Metrics.Api;
using Microsoft.Extensions.Logging;

namespace ZombiePlague.Core.Metrics;

internal sealed class MetricsServiceProxy(ILogger<MetricsServiceProxy> logger) : IMetricsService
{
    private IMetricsService? _service;

    public void Initialize(IMetricsService service)
    {
        _service = service;
    }

    public void Uninitialize()
    {
        _service = null;
    }

    public void Track(string eventName, ulong? steamId = null, object? properties = null)
    {
        try
        {
            _service?.Track(eventName, steamId, properties);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Metrics event '{EventKey}' could not be passed to Metrics.Core.",
                eventName
            );
        }
    }
}
