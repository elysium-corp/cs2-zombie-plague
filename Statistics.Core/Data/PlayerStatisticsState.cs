using System.Diagnostics;

namespace Statistics.Core.Data;

internal sealed class PlayerStatisticsState
{
    private long _playTimeRemainderTicks;

    private long _sessionCheckpointTimestamp;

    private bool _isCurrentConnectionCounted;

    public string LastKnownName { get; private set; } = string.Empty;

    public DateTime FirstSeenAtUtc { get; private set; }

    public DateTime LastSeenAtUtc { get; private set; }

    public bool IsConnected { get; private set; }

    public long SessionsCount { get; private set; }

    public long PlayTimeSeconds { get; private set; }

    public long RoundsPlayed { get; private set; }

    public long RoundsAsHuman { get; private set; }

    public long RoundsAsZombie { get; private set; }

    public long ZombiesKilled { get; private set; }

    public long HeadshotZombieKills { get; private set; }

    public long InfectionsMade { get; private set; }

    public long TimesInfected { get; private set; }

    public long DeathsAsHuman { get; private set; }

    public long DeathsAsZombie { get; private set; }

    public long DamageToZombies { get; private set; }

    public long DamageToHumans { get; private set; }

    public long SurvivedRounds { get; private set; }

    public long HumanWins { get; private set; }

    public long ZombieWins { get; private set; }

    public long FirstZombieRounds { get; private set; }

    public long LastHumanRounds { get; private set; }

    public long LastHumanSurvivals { get; private set; }

    public long BestKillStreak { get; private set; }

    public long BestInfectionStreak { get; private set; }

    public long CurrentKillStreak { get; private set; }

    public long CurrentInfectionStreak { get; private set; }

    public void Connect(
        string playerName,
        DateTime nowUtc,
        long timestamp,
        bool countSession
    )
    {
        if (!IsConnected)
        {
            IsConnected = true;
            _isCurrentConnectionCounted = countSession;
            _sessionCheckpointTimestamp = timestamp;

            if (countSession)
            {
                SessionsCount++;
            }
        }
        else if (countSession && !_isCurrentConnectionCounted)
        {
            _isCurrentConnectionCounted = true;
            SessionsCount++;
        }

        LastKnownName = playerName;
        LastSeenAtUtc = nowUtc;

        if (FirstSeenAtUtc == default)
        {
            FirstSeenAtUtc = nowUtc;
        }
    }

    public void Checkpoint(string playerName, DateTime nowUtc, long timestamp)
    {
        LastKnownName = playerName;
        LastSeenAtUtc = nowUtc;

        if (!IsConnected)
        {
            return;
        }

        AddElapsedPlayTime(timestamp);
    }

    public void Disconnect(string playerName, DateTime nowUtc, long timestamp)
    {
        Checkpoint(playerName, nowUtc, timestamp);

        IsConnected = false;
        _isCurrentConnectionCounted = false;
    }

    public void Merge(PlayerStatisticsSnapshot loaded)
    {
        ArgumentNullException.ThrowIfNull(loaded);

        if (string.IsNullOrWhiteSpace(LastKnownName))
        {
            LastKnownName = loaded.LastKnownName;
        }

        FirstSeenAtUtc = loaded.FirstSeenAtUtc;

        SessionsCount += loaded.SessionsCount;
        PlayTimeSeconds += loaded.PlayTimeSeconds;
        RoundsPlayed += loaded.RoundsPlayed;
        RoundsAsHuman += loaded.RoundsAsHuman;
        RoundsAsZombie += loaded.RoundsAsZombie;
        ZombiesKilled += loaded.ZombiesKilled;
        HeadshotZombieKills += loaded.HeadshotZombieKills;
        InfectionsMade += loaded.InfectionsMade;
        TimesInfected += loaded.TimesInfected;
        DeathsAsHuman += loaded.DeathsAsHuman;
        DeathsAsZombie += loaded.DeathsAsZombie;
        DamageToZombies += loaded.DamageToZombies;
        DamageToHumans += loaded.DamageToHumans;
        SurvivedRounds += loaded.SurvivedRounds;
        HumanWins += loaded.HumanWins;
        ZombieWins += loaded.ZombieWins;
        FirstZombieRounds += loaded.FirstZombieRounds;
        LastHumanRounds += loaded.LastHumanRounds;
        LastHumanSurvivals += loaded.LastHumanSurvivals;

        BestKillStreak = Math.Max(BestKillStreak, loaded.BestKillStreak);
        BestInfectionStreak = Math.Max(BestInfectionStreak, loaded.BestInfectionStreak);
    }

    public void RecordDamageToZombies(int damage)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(damage);

        DamageToZombies += damage;
    }

    public void RecordDamageToHumans(int damage)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(damage);

        DamageToHumans += damage;
    }

    public void RecordZombieKill(bool isHeadshot)
    {
        ZombiesKilled++;

        if (isHeadshot)
        {
            HeadshotZombieKills++;
        }

        CurrentKillStreak++;
        BestKillStreak = Math.Max(BestKillStreak, CurrentKillStreak);
    }

    public void RecordInfectionMade()
    {
        InfectionsMade++;
        CurrentInfectionStreak++;
        BestInfectionStreak = Math.Max(BestInfectionStreak, CurrentInfectionStreak);
    }

    public void RecordTimesInfected()
    {
        TimesInfected++;
    }

    public void RecordDeath(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Human:
                DeathsAsHuman++;
                break;

            case PlayerRole.Zombie:
                DeathsAsZombie++;
                break;
        }

        ResetStreaks();
    }

    public void RecordRound(RoundStatisticsResult result)
    {
        RoundsPlayed++;

        if (result.WasHuman)
        {
            RoundsAsHuman++;
        }

        if (result.WasZombie)
        {
            RoundsAsZombie++;
        }

        if (result.WasFirstZombie)
        {
            FirstZombieRounds++;
        }

        if (result.WasLastHuman)
        {
            LastHumanRounds++;
        }

        if (result.SurvivedRound)
        {
            SurvivedRounds++;
        }

        if (result.HumanWon)
        {
            HumanWins++;
        }

        if (result.ZombieWon)
        {
            ZombieWins++;
        }

        if (result.LastHumanSurvived)
        {
            LastHumanSurvivals++;
        }

        ResetStreaks();
    }

    public void ResetStreaks()
    {
        CurrentKillStreak = 0;
        CurrentInfectionStreak = 0;
    }

    public PlayerStatisticsSnapshot CreateSnapshot()
    {
        return new PlayerStatisticsSnapshot
        {
            LastKnownName = LastKnownName,
            FirstSeenAtUtc = FirstSeenAtUtc,
            LastSeenAtUtc = LastSeenAtUtc,
            SessionsCount = SessionsCount,
            PlayTimeSeconds = PlayTimeSeconds,
            RoundsPlayed = RoundsPlayed,
            RoundsAsHuman = RoundsAsHuman,
            RoundsAsZombie = RoundsAsZombie,
            ZombiesKilled = ZombiesKilled,
            HeadshotZombieKills = HeadshotZombieKills,
            InfectionsMade = InfectionsMade,
            TimesInfected = TimesInfected,
            DeathsAsHuman = DeathsAsHuman,
            DeathsAsZombie = DeathsAsZombie,
            DamageToZombies = DamageToZombies,
            DamageToHumans = DamageToHumans,
            SurvivedRounds = SurvivedRounds,
            HumanWins = HumanWins,
            ZombieWins = ZombieWins,
            FirstZombieRounds = FirstZombieRounds,
            LastHumanRounds = LastHumanRounds,
            LastHumanSurvivals = LastHumanSurvivals,
            BestKillStreak = BestKillStreak,
            BestInfectionStreak = BestInfectionStreak
        };
    }

    private void AddElapsedPlayTime(long timestamp)
    {
        if (_sessionCheckpointTimestamp == 0)
        {
            _sessionCheckpointTimestamp = timestamp;

            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(_sessionCheckpointTimestamp, timestamp);
        var elapsedTicks = elapsed.Ticks + _playTimeRemainderTicks;

        PlayTimeSeconds += elapsedTicks / TimeSpan.TicksPerSecond;
        _playTimeRemainderTicks = elapsedTicks % TimeSpan.TicksPerSecond;
        _sessionCheckpointTimestamp = timestamp;
    }
}

