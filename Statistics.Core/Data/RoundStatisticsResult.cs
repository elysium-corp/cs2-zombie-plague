namespace Statistics.Core.Data;

internal readonly record struct RoundStatisticsResult(
    bool WasHuman,
    bool WasZombie,
    bool WasFirstZombie,
    bool WasLastHuman,
    bool SurvivedRound,
    bool HumanWon,
    bool ZombieWon,
    bool LastHumanSurvived
);

