using System.Text.Json;
using Metrics.Core.Models;

namespace Metrics.Core.Tests;

public sealed class MetricsSerializationTests
{
    [Fact]
    public void EventEnvelope_SerializesToFluteIngestionContract()
    {
        var metricEvent = new MetricEventEnvelope
        {
            EventId = "0123456789abcdef0123456789abcdef",
            EventKey = "class_selected",
            SchemaVersion = 1,
            OccurredAt = new DateTimeOffset(2026, 8, 26, 12, 42, 15, TimeSpan.Zero),
            ServerId = 1,
            SteamId = "76561198000000000",
            SessionId = "session_0123456789abcdef",
            Map = "ze_example",
            ReleaseVersion = "0.1.0",
            Properties = JsonSerializer.SerializeToElement(
                new
                {
                    class_id = "zombie_cleric",
                    class_name = "Cleric",
                    class_type = "zombie"
                },
                MetricsJson.Options
            )
        };

        var json = JsonSerializer.Serialize(metricEvent, MetricsJson.Options);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("class_selected", root.GetProperty("eventKey").GetString());
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(1, root.GetProperty("serverId").GetInt32());
        Assert.Equal("76561198000000000", root.GetProperty("steamId").GetString());
        Assert.Equal("zombie_cleric", root.GetProperty("properties").GetProperty("class_id").GetString());
        Assert.False(root.TryGetProperty("roundId", out _));
    }
}
