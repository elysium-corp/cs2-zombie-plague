namespace Metrics.Core.Config;

internal sealed class MetricsConfig
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://elysiumcs.su";

    public string ApiSecret { get; set; } = string.Empty;

    public int ServerId { get; set; } = 1;

    public string ReleaseVersion { get; set; } = "0.1.0";

    public bool IncludeMap { get; set; } = true;

    public bool IncludeSessionId { get; set; } = true;

    public int BatchSize { get; set; } = 50;

    public int QueueCapacity { get; set; } = 5_000;

    public int FlushIntervalMilliseconds { get; set; } = 2_000;

    public int RequestTimeoutSeconds { get; set; } = 10;

    public int RetryCount { get; set; } = 4;

    public int RetryBaseDelayMilliseconds { get; set; } = 500;

    public int RetryMaxDelaySeconds { get; set; } = 15;

    public int MaxEventBytes { get; set; } = 16_384;

    public bool PersistentSpoolEnabled { get; set; } = true;

    public string SpoolFileName { get; set; } = "metrics-spool.jsonl";

    public long MaxSpoolBytes { get; set; } = 52_428_800;

    public Dictionary<string, int> SchemaVersions { get; set; } = new(StringComparer.Ordinal)
    {
        ["class_selected"] = 1
    };
}
