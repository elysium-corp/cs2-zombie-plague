using System.Diagnostics;

namespace Statistics.Core.Data;

internal sealed class PlayerStatisticsState
{
    private long _playTimeRemainderTicks;

    private long _sessionCheckpointTimestamp;

    public string LastKnownName { get; private set; } = string.Empty;

    public DateTime FirstSeenAtUtc { get; private set; }

    public DateTime LastSeenAtUtc { get; private set; }

    public bool IsConnected { get; private set; }

    public long Points { get; private set; }

    public long PlayTimeSeconds { get; private set; }

    public long ZombiesKilled { get; private set; }

    public long InfectionsMade { get; private set; }

    public long TimesInfected { get; private set; }

    public long Deaths { get; private set; }

    public long HumanWins { get; private set; }

    public long ZombieWins { get; private set; }

    public long BestKillStreak { get; private set; }

    public long BestInfectionStreak { get; private set; }

    public void Connect(string playerName, DateTime nowUtc, long timestamp)
    {
        if (!IsConnected)
        {
            IsConnected = true;
            _sessionCheckpointTimestamp = timestamp;
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

        if (IsConnected)
        {
            AddElapsedPlayTime(timestamp);
        }
    }

    public void Disconnect(string playerName, DateTime nowUtc, long timestamp)
    {
        Checkpoint(playerName, nowUtc, timestamp);
        IsConnected = false;
    }

    public void Merge(PlayerStatisticsSnapshot loaded)
    {
        ArgumentNullException.ThrowIfNull(loaded);

        if (string.IsNullOrWhiteSpace(LastKnownName))
        {
            LastKnownName = loaded.LastKnownName;
        }

        FirstSeenAtUtc = loaded.FirstSeenAtUtc;

        Points = AddPoints(loaded.Points, Points);
        PlayTimeSeconds += loaded.PlayTimeSeconds;
        ZombiesKilled += loaded.ZombiesKilled;
        InfectionsMade += loaded.InfectionsMade;
        TimesInfected += loaded.TimesInfected;
        Deaths += loaded.Deaths;
        HumanWins += loaded.HumanWins;
        ZombieWins += loaded.ZombieWins;

        BestKillStreak = Math.Max(BestKillStreak, loaded.BestKillStreak);
        BestInfectionStreak = Math.Max(BestInfectionStreak, loaded.BestInfectionStreak);
    }

    public void RecordZombieKill(long currentStreak)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(currentStreak);

        ZombiesKilled++;
        BestKillStreak = Math.Max(BestKillStreak, currentStreak);
    }

    public void RecordInfectionMade(long currentStreak)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(currentStreak);

        InfectionsMade++;
        BestInfectionStreak = Math.Max(BestInfectionStreak, currentStreak);
    }

    public void RecordTimesInfected()
    {
        TimesInfected++;
    }

    public void RecordDeath()
    {
        Deaths++;
    }

    public long RecordRound(RoundStatisticsResult result)
    {
        var previousPoints = Points;

        Points = AddPoints(Points, result.PointsDelta);

        if (result.HumanWon)
        {
            HumanWins++;
        }

        if (result.ZombieWon)
        {
            ZombieWins++;
        }

        return Points - previousPoints;
    }

    public PlayerStatisticsSnapshot CreateSnapshot()
    {
        return new PlayerStatisticsSnapshot
        {
            LastKnownName = LastKnownName,
            FirstSeenAtUtc = FirstSeenAtUtc,
            LastSeenAtUtc = LastSeenAtUtc,
            Points = Points,
            PlayTimeSeconds = PlayTimeSeconds,
            ZombiesKilled = ZombiesKilled,
            InfectionsMade = InfectionsMade,
            TimesInfected = TimesInfected,
            Deaths = Deaths,
            HumanWins = HumanWins,
            ZombieWins = ZombieWins,
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

    private static long AddPoints(long currentPoints, long delta)
    {
        var result = (decimal)currentPoints + delta;

        if (result <= 0)
        {
            return 0;
        }

        return result >= long.MaxValue
            ? long.MaxValue
            : (long)result;
    }
}
