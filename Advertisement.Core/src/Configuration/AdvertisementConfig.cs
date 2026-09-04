namespace Advertisement.Core.Configuration;

internal sealed class AdvertisementConfig
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 90;
    public int RefreshIntervalSeconds { get; set; } = 30;
    public int InitialDelaySeconds { get; set; } = 45;
    public string OrderMode { get; set; } = "sequential";
    public bool ExcludeBotsFromPlayers { get; set; } = true;

    public List<FallbackMessageConfig> Messages { get; set; } =
    [
        new()
        {
            Key = "Discord",
            Name = "Discord",
            LocalizationKey = "Advertisement.Messages.Discord",
            Tag = "Elysium",
        },
    ];
}

internal sealed class FallbackMessageConfig
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LocalizationKey { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public string Type { get; set; } = "information";
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
    public int Weight { get; set; } = 100;
    public int SortOrder { get; set; }
    public int? IntervalSeconds { get; set; }
    public string DispatchMode { get; set; } = "periodic";
    public List<string> DailyTimes { get; set; } = [];
    public string? DailyStartTime { get; set; }
    public string? DailyEndTime { get; set; }
    public string AudienceType { get; set; } = "all";
    public string? AudienceGroup { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}
