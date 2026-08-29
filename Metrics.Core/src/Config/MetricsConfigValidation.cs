namespace Metrics.Core.Config;

internal static class MetricsConfigValidation
{
    private const int DefaultIngestionPayloadBytes = 1_048_576;
    private const int EstimatedEnvelopeBytes = 1_024;

    public static bool TryValidate(MetricsConfig config, out string error)
    {
        if (!config.Enabled)
        {
            error = string.Empty;

            return true;
        }

        if (!TryBuildIngestionUri(config.BaseUrl, config.AllowInsecureLoopbackHttp, out _))
        {
            error = "BaseUrl must use HTTPS; loopback HTTP requires AllowInsecureLoopbackHttp.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(config.ApiSecret))
        {
            error = "ApiSecret is empty.";

            return false;
        }

        if (config.ServerId < 1)
        {
            error = "ServerId must be greater than zero.";

            return false;
        }

        if (config.BatchSize is < 1 or > 1_000)
        {
            error = "BatchSize must be between 1 and 1000.";

            return false;
        }

        if (config.QueueCapacity < config.BatchSize || config.QueueCapacity > 100_000)
        {
            error = "QueueCapacity must be at least BatchSize and no greater than 100000.";

            return false;
        }

        if (config.FlushIntervalMilliseconds is < 100 or > 60_000)
        {
            error = "FlushIntervalMilliseconds must be between 100 and 60000.";

            return false;
        }

        if (config.RequestTimeoutSeconds is < 1 or > 60)
        {
            error = "RequestTimeoutSeconds must be between 1 and 60.";

            return false;
        }

        if (config.RetryCount is < 0 or > 10)
        {
            error = "RetryCount must be between 0 and 10.";

            return false;
        }

        if (config.RetryBaseDelayMilliseconds is < 50 or > 10_000)
        {
            error = "RetryBaseDelayMilliseconds must be between 50 and 10000.";

            return false;
        }

        if (config.RetryMaxDelaySeconds is < 1 or > 120)
        {
            error = "RetryMaxDelaySeconds must be between 1 and 120.";

            return false;
        }

        if (config.MaxEventBytes is < 1_024 or > 262_144)
        {
            error = "MaxEventBytes must be between 1024 and 262144.";

            return false;
        }

        var estimatedBatchBytes = (long)config.BatchSize * (config.MaxEventBytes + EstimatedEnvelopeBytes);
        if (estimatedBatchBytes > DefaultIngestionPayloadBytes)
        {
            error = "BatchSize and MaxEventBytes can exceed the default 1 MiB Flute ingestion limit.";

            return false;
        }

        if (config.PersistentSpoolEnabled)
        {
            if (string.IsNullOrWhiteSpace(config.SpoolFileName) ||
                !string.Equals(Path.GetFileName(config.SpoolFileName), config.SpoolFileName, StringComparison.Ordinal))
            {
                error = "SpoolFileName must be a file name without a directory path.";

                return false;
            }

            if (config.MaxSpoolBytes < 1_048_576)
            {
                error = "MaxSpoolBytes must be at least 1 MiB.";

                return false;
            }
        }

        if (config.SchemaVersions is null || config.SchemaVersions.Count == 0)
        {
            error = "SchemaVersions must contain at least one event contract.";

            return false;
        }

        foreach (var (eventKey, schemaVersion) in config.SchemaVersions)
        {
            if (!IsValidEventKey(eventKey) || schemaVersion < 1)
            {
                error = $"SchemaVersions contains an invalid contract: '{eventKey}' v{schemaVersion}.";

                return false;
            }
        }

        error = string.Empty;

        return true;
    }

    public static bool TryBuildIngestionUri(string baseUrl, out Uri ingestionUri)
        => TryBuildIngestionUri(baseUrl, false, out ingestionUri);

    public static bool TryBuildIngestionUri(
        string baseUrl,
        bool allowInsecureLoopbackHttp,
        out Uri ingestionUri)
    {
        if (!string.IsNullOrWhiteSpace(baseUrl) &&
            Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) &&
            string.IsNullOrEmpty(baseUri.UserInfo) &&
            (string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
             allowInsecureLoopbackHttp &&
             string.Equals(baseUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             baseUri.IsLoopback))
        {
            ingestionUri = new Uri(baseUri, "api/metrics/v1/events");

            return true;
        }

        ingestionUri = null!;

        return false;
    }

    public static bool IsValidEventKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value[0] is < 'a' or > 'z')
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
