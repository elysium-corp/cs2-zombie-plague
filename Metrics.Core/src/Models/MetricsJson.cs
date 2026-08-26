using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metrics.Core.Models;

internal static class MetricsJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
}
