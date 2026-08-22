namespace Statistics.Core.Data;

internal sealed class PlayerStatisticsSnapshot
{
    public string LastKnownName { get; init; } = string.Empty;

    public DateTime FirstSeenAtUtc { get; init; }

    public DateTime LastSeenAtUtc { get; init; }

    public long SessionsCount { get; init; }

    public long PlayTimeSeconds { get; init; }

    public long RoundsPlayed { get; init; }

    public long RoundsAsHuman { get; init; }

    public long RoundsAsZombie { get; init; }

    public long ZombiesKilled { get; init; }

    public long HeadshotZombieKills { get; init; }

    public long InfectionsMade { get; init; }

    public long TimesInfected { get; init; }

    public long DeathsAsHuman { get; init; }

    public long DeathsAsZombie { get; init; }

    public long DamageToZombies { get; init; }

    public long DamageToHumans { get; init; }

    public long SurvivedRounds { get; init; }

    public long HumanWins { get; init; }

    public long ZombieWins { get; init; }

    public long FirstZombieRounds { get; init; }

    public long LastHumanRounds { get; init; }

    public long LastHumanSurvivals { get; init; }

    public long BestKillStreak { get; init; }

    public long BestInfectionStreak { get; init; }
}

