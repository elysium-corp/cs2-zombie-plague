namespace Statistics.Core.Data;

internal sealed class PlayerStatisticsSnapshot
{
    public string LastKnownName { get; init; } = string.Empty;

    public DateTime FirstSeenAtUtc { get; init; }

    public DateTime LastSeenAtUtc { get; init; }

    public long Points { get; init; }

    public long PlayTimeSeconds { get; init; }

    public long ZombiesKilled { get; init; }

    public long InfectionsMade { get; init; }

    public long TimesInfected { get; init; }

    public long Deaths { get; init; }

    public long HumanWins { get; init; }

    public long ZombieWins { get; init; }

    public long BestKillStreak { get; init; }

    public long BestInfectionStreak { get; init; }
}
