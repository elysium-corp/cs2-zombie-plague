namespace Advertisement.Core.Configuration;

internal sealed class AdvertisementConfig
{
    public bool Enabled { get; set; } = true;
    public long ServerId { get; set; } = 1;
    public string DefaultLocale { get; set; } = "ru";
    public List<string> AllowedLocales { get; set; } = ["ru", "en", "uk", "pl", "de"];
    public int IntervalSeconds { get; set; } = 90;
    public int RefreshIntervalSeconds { get; set; } = 30;
    public int InitialDelaySeconds { get; set; } = 45;
    public string OrderMode { get; set; } = "sequential";
    public bool ExcludeBotsFromPlayers { get; set; } = true;

    public Dictionary<string, string> Colors { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = "default",
        ["accent"] = "lightblue",
        ["warning"] = "red",
        ["success"] = "green",
        ["important"] = "orange",
        ["muted"] = "gray",
    };

    public List<FallbackTagConfig> Tags { get; set; } =
    [
        new()
        {
            Key = "elysium",
            Color = "purple",
            Translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ru"] = "Elysium",
                ["en"] = "Elysium",
            },
        },
    ];

    public List<FallbackMessageConfig> Messages { get; set; } =
    [
        new()
        {
            Key = "discord",
            Name = "Discord",
            Tag = "elysium",
            Translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ru"] = "Наш Discord: {accent}discord.gg/elysium{/accent}",
                ["en"] = "Our Discord: {accent}discord.gg/elysium{/accent}",
            },
        },
    ];
}

internal sealed class FallbackTagConfig
{
    public string Key { get; set; } = string.Empty;
    public string Color { get; set; } = "default";
    public Dictionary<string, string> Translations { get; set; } = [];
}

internal sealed class FallbackMessageConfig
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public string Type { get; set; } = "information";
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
    public int Weight { get; set; } = 100;
    public int SortOrder { get; set; }
    public int? IntervalSeconds { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public Dictionary<string, string> Translations { get; set; } = [];
}
