namespace Statistics.Core.Data;

internal sealed class RoundParticipantState
{
    public PlayerRole CurrentRole { get; private set; }

    public long ZombiesKilled { get; private set; }

    public long InfectionsMade { get; private set; }

    public long TimesInfected { get; private set; }

    public long Deaths { get; private set; }

    public long CurrentKillStreak { get; private set; }

    public long CurrentInfectionStreak { get; private set; }

    public long BestKillStreak { get; private set; }

    public long BestInfectionStreak { get; private set; }

    public void SetRole(PlayerRole role)
    {
        if (CurrentRole is not PlayerRole.None && CurrentRole != role)
        {
            ResetStreaks();
        }

        CurrentRole = role;
    }

    public long RecordZombieKill()
    {
        ZombiesKilled++;
        CurrentKillStreak++;
        BestKillStreak = Math.Max(BestKillStreak, CurrentKillStreak);

        return CurrentKillStreak;
    }

    public long RecordInfectionMade()
    {
        InfectionsMade++;
        CurrentInfectionStreak++;
        BestInfectionStreak = Math.Max(BestInfectionStreak, CurrentInfectionStreak);

        return CurrentInfectionStreak;
    }

    public void RecordTimesInfected()
    {
        TimesInfected++;
    }

    public void RecordDeath()
    {
        Deaths++;
        ResetStreaks();
    }

    public RoundPointsContext CreatePointsContext(bool humanWon, bool zombieWon)
    {
        return new RoundPointsContext(
            ZombiesKilled: ZombiesKilled,
            InfectionsMade: InfectionsMade,
            TimesInfected: TimesInfected,
            Deaths: Deaths,
            HumanWin: humanWon,
            ZombieWin: zombieWon,
            BestKillStreak: BestKillStreak,
            BestInfectionStreak: BestInfectionStreak
        );
    }

    private void ResetStreaks()
    {
        CurrentKillStreak = 0;
        CurrentInfectionStreak = 0;
    }
}
