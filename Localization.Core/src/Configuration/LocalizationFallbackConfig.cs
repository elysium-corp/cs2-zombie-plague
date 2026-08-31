namespace Localization.Core.Configuration;

internal sealed class LocalizationFallbackConfig
{
    public int SchemaVersion { get; set; } = 2;
    public long Version { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UnixEpoch;
    public string Checksum { get; set; } = string.Empty;
    public string ServerFallbackLanguage { get; set; } = "ru";
    public List<string> Languages { get; set; } = ["ru", "en", "de", "pl"];
    public int RefreshIntervalSeconds { get; set; } = 30;
    public bool LocalCacheEnabled { get; set; } = true;
    public bool LogMissingKeys { get; set; } = true;
    public Dictionary<string, Dictionary<string, string>> Entries { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<LocalizationFallbackParameterConfig>> Parameters { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class LocalizationFallbackParameterConfig
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public bool Required { get; set; } = true;
    public string? Description { get; set; }
    public string Example { get; set; } = string.Empty;
}
