namespace Statistics.Core.Data;

internal readonly record struct RoundPointsContext(
    long ZombiesKilled,
    long InfectionsMade,
    long TimesInfected,
    long Deaths,
    bool HumanWin,
    bool ZombieWin,
    long BestKillStreak,
    long BestInfectionStreak
);
