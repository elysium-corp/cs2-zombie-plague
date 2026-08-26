using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metrics.Core.Models;

internal sealed record MetricEventEnvelope
{
    public required string EventId { get; init; }

    public required string EventKey { get; init; }

    public required int SchemaVersion { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required int ServerId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SteamId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SessionId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RoundId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Map { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReleaseVersion { get; init; }

    public required JsonElement Properties { get; init; }
}

internal sealed record MetricBatchRequest
{
    public required IReadOnlyCollection<MetricEventEnvelope> Events { get; init; }
}
