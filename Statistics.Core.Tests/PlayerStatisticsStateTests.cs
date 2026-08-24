using Statistics.Core.Data;

namespace Statistics.Core.Tests;

public sealed class PlayerStatisticsStateTests
{
    [Fact]
    public void RoundDeltaCannotMakeTotalPointsNegative()
    {
        var state = new PlayerStatisticsState();
        var gained = state.RecordRound(new RoundStatisticsResult(10, false, false));

        var lost = state.RecordRound(new RoundStatisticsResult(-15, false, false));

        Assert.Equal(10, gained);
        Assert.Equal(-10, lost);
        Assert.Equal(0, state.Points);
    }

    [Fact]
    public void RoundResultRecordsOnlyMatchingWinCounters()
    {
        var state = new PlayerStatisticsState();

        state.RecordRound(new RoundStatisticsResult(0, true, false));
        state.RecordRound(new RoundStatisticsResult(0, false, true));
        state.RecordRound(new RoundStatisticsResult(0, false, false));

        Assert.Equal(1, state.HumanWins);
        Assert.Equal(1, state.ZombieWins);
    }

    [Fact]
    public void DatabaseSnapshotMergesWithEventsCollectedDuringLoad()
    {
        var state = new PlayerStatisticsState();
        state.RecordZombieKill(currentStreak: 1);
        state.RecordRound(new RoundStatisticsResult(20, false, false));

        state.Merge(new PlayerStatisticsSnapshot
        {
            LastKnownName = "Player",
            FirstSeenAtUtc = DateTime.UnixEpoch,
            LastSeenAtUtc = DateTime.UnixEpoch,
            Points = 100,
            ZombiesKilled = 5,
            BestKillStreak = 4
        });

        Assert.Equal(120, state.Points);
        Assert.Equal(6, state.ZombiesKilled);
        Assert.Equal(4, state.BestKillStreak);
    }
}
