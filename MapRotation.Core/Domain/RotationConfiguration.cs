using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MapRotation.Core.Domain;

internal sealed record RotationSettings
{
    public int Id { get; init; } = 1;
    public int MapDurationSeconds { get; init; } = 2700;
    public bool ScheduledVoteEnabled { get; init; } = true;
    public int ScheduledVoteBeforeSeconds { get; init; } = 300;
    public int VoteDurationSeconds { get; init; } = 20;
    public int VoteOptionsCount { get; init; } = 6;
    public int NominationSlots { get; init; } = 3;
    public bool RtvEnabled { get; init; } = true;
    public double RtvRatio { get; init; } = .6;
    public int RtvMinVotes { get; init; } = 3;
    public int RtvMinPlayers { get; init; } = 4;
    public int RtvDelaySeconds { get; init; } = 300;
    public string RtvChangeMode { get; init; } = "end_of_round";
    public int RtvChangeDelaySeconds { get; init; }
    public bool NominationsEnabled { get; init; } = true;
    public bool ExcludeBots { get; init; } = true;
    public bool IncludeSpectators { get; init; } = true;
    public bool AllowSameMap { get; init; }
    public int RecentMapsExcluded { get; init; } = 3;
    public int FinalRoundTimeoutSeconds { get; init; } = 600;
    public long? FallbackMapId { get; init; }
    public int RefreshIntervalSeconds { get; init; } = 15;
    public long ConfigurationVersion { get; init; } = 1;
    public int MenuItemsPerPage { get; init; } = 5;
    public string HudSettings { get; init; } = "{}";

    public void Validate()
    {
        if (Id != 1 || MapDurationSeconds is < 30 or > 604800 || ScheduledVoteBeforeSeconds < 0
            || ScheduledVoteBeforeSeconds > MapDurationSeconds || VoteDurationSeconds is < 5 or > 300
            || VoteOptionsCount is < 1 or > 30 || NominationSlots < 0 || NominationSlots > VoteOptionsCount
            || !double.IsFinite(RtvRatio) || RtvRatio is <= 0 or > 1 || RtvMinVotes < 1 || RtvMinPlayers < 1
            || RtvDelaySeconds < 0 || RtvChangeMode is not ("end_of_round" or "immediate")
            || RtvChangeDelaySeconds is < 0 or > 600 || RecentMapsExcluded is < 0 or > 1000
            || FinalRoundTimeoutSeconds is < 1 or > 7200 || RefreshIntervalSeconds is < 1 or > 3600
            || MenuItemsPerPage is < 1 or > 10 || ConfigurationVersion < 1) throw new ArgumentException("Некорректные настройки MapRotation");
    }
}

internal sealed record RotationMap
{
    public long Id { get; init; }
    public string Key { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string MapName { get; init; } = "";
    public long? WorkshopId { get; init; }
    public bool Enabled { get; init; } = true;
    public double Weight { get; init; } = 1;
    public int? CooldownMaps { get; init; }
    public int SortOrder { get; init; }
    public bool AllowNomination { get; init; } = true;
    public bool AllowVote { get; init; } = true;
    public bool AllowAutoRotation { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? HudImagePath { get; init; }
    public string EngineTarget => WorkshopId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? MapName;

    public bool IsSafe => Id > 0 && Regex.IsMatch(Key, "^[A-Za-z0-9_-]{1,64}$")
        && DisplayName.Length is > 0 and <= 128 && !DisplayName.Any(char.IsControl)
        && Regex.IsMatch(MapName, "^[A-Za-z0-9_/-]{1,128}$") && !MapName.StartsWith('/')
        && WorkshopId is null or > 0 && double.IsFinite(Weight) && Weight is > 0 and <= 1_000_000
        && CooldownMaps is null or >= 0 and <= 1000
        && (HudImagePath is null || Regex.IsMatch(HudImagePath,
            "\\Apanorama/images/custom_game/elysium/assets/[a-f0-9]{64}_png\\.vtex\\z"));

    public bool IsCurrent(string mapName, string workshopId) =>
        WorkshopId is { } id && id.ToString() == workshopId
        || string.Equals(MapName, mapName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(MapName.Split('/')[^1], mapName.Split('/')[^1], StringComparison.OrdinalIgnoreCase);
}

internal sealed record RotationConfiguration(RotationSettings Settings, ImmutableArray<RotationMap> Maps)
{
    public static RotationConfiguration Empty { get; } = new(new(), []);
    public static RotationConfiguration Create(RotationSettings settings, IEnumerable<RotationMap> maps)
    {
        settings.Validate();
        var snapshot = maps.ToImmutableArray();
        if (snapshot.Any(map => !map.IsSafe) || snapshot.Select(map => map.Id).Distinct().Count() != snapshot.Length
            || snapshot.Select(map => map.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != snapshot.Length
            || snapshot.Select(map => map.EngineTarget).Distinct(StringComparer.OrdinalIgnoreCase).Count() != snapshot.Length)
            throw new ArgumentException("Некорректный или дублирующийся каталог MapRotation");
        return new(settings, snapshot);
    }
}

/// <summary>Источник случайных значений для воспроизводимой проверки выбора карт.</summary>
internal interface IRotationRandom
{
    /// <summary>Равномерное значение в полуинтервале [0, 1).</summary>
    double NextDouble();
}

internal sealed class RotationRandom : IRotationRandom
{
    public double NextDouble() => Random.Shared.NextDouble();
}
