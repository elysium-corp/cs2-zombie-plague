namespace Statistics.Core.Data;

internal readonly record struct RoundStatisticsResult(
    long PointsDelta,
    bool HumanWon,
    bool ZombieWon
);
