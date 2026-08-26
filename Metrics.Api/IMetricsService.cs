namespace Metrics.Api;

/// <summary>
/// Non-blocking entry point for sending game events to Elysium Metrics.
/// Implementations must never perform network or disk I/O on the caller's thread.
/// </summary>
public interface IMetricsService
{
    public static readonly string SharedApiKey = "Metrics.Api.IMetricsService";

    /// <summary>
    /// Queues an event for background delivery.
    /// </summary>
    /// <param name="eventName">Event key configured in Flute, for example <c>class_selected</c>.</param>
    /// <param name="steamId">Optional SteamID64 associated with the event.</param>
    /// <param name="properties">Anonymous object or DTO matching the configured event schema.</param>
    public void Track(string eventName, ulong? steamId = null, object? properties = null);
}
