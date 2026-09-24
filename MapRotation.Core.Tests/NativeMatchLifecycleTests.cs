using System.Text.Json;
using MapRotation.Api;
using MapRotation.Core.Domain;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class NativeMatchLifecycleTests
{
    [Theory]
    [InlineData(0, 100, 10, null)]
    [InlineData(6, 160, 100, 300d)]
    [InlineData(6, 500, 100, 0d)]
    [InlineData(6, 50, 100, null)]
    public void TimeLimitUsesGameStartTimeAndZeroMeansUnlimited(double minutes, double now, double started, double? expected) =>
        Assert.Equal(expected, NativeMatchProgress.RemainingSeconds(minutes, now, started));

    [Theory]
    [InlineData(0, 0, false, 0, 0, null)]
    [InlineData(30, 28, false, 0, 14, 2)]
    [InlineData(30, 16, true, 0, 14, 2)]
    [InlineData(30, 16, false, 0, 14, 14)]
    [InlineData(0, 9, false, 10, 8, 2)]
    [InlineData(30, 9, false, 10, 8, 2)]
    public void RoundPredictionHonorsRoundLimitClinchAndWinLimit(int max, int played, bool clinch, int wins, int score, int? expected) =>
        Assert.Equal(expected, NativeMatchProgress.RemainingRounds(max, played, clinch, wins, score));

    [Theory]
    [InlineData(300d, null)]
    [InlineData(null, 2)]
    public void NativeLimitsStartOneScheduledVoteBeforeThePluginDeadline(double? seconds, int? rounds)
    {
        var f = new RotationEngineTests.Fixture(new() { ScheduledVoteEnabled = true });
        var deadline = f.Engine.Deadline;
        f.Engine.ObserveNativeMatch(new(false, true, false, seconds, rounds));
        var vote = Assert.IsType<VoteState>(f.Engine.Vote);
        f.Engine.ObserveNativeMatch(new(false, true, false, seconds, rounds));

        Assert.Equal(vote.Id, f.Engine.Vote!.Id);
        Assert.Equal(NextMapSource.ScheduledVote, vote.Source);
        Assert.Equal(deadline, f.Engine.Deadline);
        Assert.Empty(f.Changes);
    }

    [Theory]
    [InlineData(true, true, 10d, 1)]
    [InlineData(false, false, 10d, 1)]
    [InlineData(false, true, null, null)]
    [InlineData(false, true, 301d, 3)]
    public void WarmupUnstartedUnlimitedAndDistantMatchesDoNotStartVotes(bool warmup, bool started, double? seconds, int? rounds)
    {
        var f = new RotationEngineTests.Fixture(new() { ScheduledVoteEnabled = true });
        f.Engine.ObserveNativeMatch(new(warmup, started, false, seconds, rounds));
        Assert.Null(f.Engine.Vote);
        Assert.Empty(f.Changes);
    }

    [Fact]
    public void NativeTimeLeftIsVisibleWithoutReplacingTheConfiguredDeadline()
    {
        var f = new RotationEngineTests.Fixture();
        var configuredDeadline = f.Engine.Deadline;
        f.Engine.ObserveNativeMatch(new(false, true, false, 120, null));
        Assert.Equal(120, f.Engine.GetStatus().TimeLeftSeconds);
        Assert.Equal(f.Clock.GetUtcNow().AddSeconds(120), f.Engine.GetStatus().Deadline);
        Assert.Equal(configuredDeadline, f.Engine.Checkpoint().Deadline);
        f.Engine.ObserveNativeMatch(new(false, true, false, null, null));
        Assert.Equal(2700, f.Engine.GetStatus().TimeLeftSeconds);
    }

    [Fact]
    public void NativeMatchEndChangesToAnAdminSelectionExactlyOnce()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.SetNext(3, false);
        f.Engine.MatchEnded();
        f.Engine.MatchEnded();
        f.Engine.RoundEnded();
        Assert.Equal(0, f.Engine.GetStatus().TimeLeftSeconds);
        Assert.Empty(f.Changes);
        f.Clock.Advance(3);
        f.Engine.Tick();
        f.Engine.Tick();
        f.Engine.MatchEnded();
        Assert.Equal(3, Assert.Single(f.Changes).Map.Id);
        Assert.False(f.Changes[0].Forced);
    }

    [Fact]
    public void MatchEndBeforeVoteCompletionKeepsFullVoteDurationAndChangesToItsWinner()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.StartVote(NextMapSource.Admin);
        var vote = f.Engine.Vote!;
        f.Clock.Advance(5);
        f.Engine.MatchEnded();
        f.Engine.RoundEnded();
        Assert.Equal(vote.EndsAt, f.Engine.Vote!.EndsAt);
        f.Engine.CastVote(1, vote.Id, 3);
        f.Clock.Advance(15);
        f.Engine.Tick();
        Assert.Null(f.Engine.Vote);
        Assert.Empty(f.Changes);
        f.Clock.Advance(3);
        f.Engine.Tick();
        Assert.Equal(3, Assert.Single(f.Changes).Map.Id);
    }

    [Fact]
    public void UnexpectedMatchEndCanStartTheConfiguredVote()
    {
        var f = new RotationEngineTests.Fixture(new() { ScheduledVoteEnabled = true });
        f.Engine.MatchEnded();
        Assert.Equal(NextMapSource.ScheduledVote, f.Engine.Vote!.Source);
        Assert.Empty(f.Changes);
    }

    [Fact]
    public void EmptyServerDefersAnEndedMatchUntilAPlayerReturns()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.SetNext(3, false);
        f.Engine.SetPlayers([]);
        f.Engine.MatchEnded();
        f.Clock.Advance(7200);
        f.Engine.Tick();
        Assert.Empty(f.Changes);
        f.Engine.SetPlayers([1]);
        f.Engine.Tick();
        f.Clock.Advance(3);
        f.Engine.Tick();
        Assert.Equal(3, Assert.Single(f.Changes).Map.Id);
    }

    [Fact]
    public void EmptyServerDoesNotSpendObservedNativeTime()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.ObserveNativeMatch(new(false, true, false, 120, 10));
        f.Engine.SetPlayers([]);
        f.Clock.Advance(7200);
        f.Engine.ObserveNativeMatch(new(false, true, false, 0, 0));
        Assert.Equal(120, f.Engine.GetStatus().TimeLeftSeconds);
        f.Engine.SetPlayers([1]);
        f.Clock.Advance(10);
        Assert.Equal(110, f.Engine.GetStatus().TimeLeftSeconds);
        Assert.Empty(f.Changes);
    }

    [Fact]
    public void HotReloadOnEmptyServerKeepsObservedNativeTime()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.ObserveNativeMatch(new(false, true, false, 120, 10));
        f.Engine.SetPlayers([]);
        var saved = JsonSerializer.Deserialize<RotationCheckpoint>(JsonSerializer.Serialize(f.Engine.Checkpoint()))!;
        f.Clock.Advance(7200);
        var restored = f.NewEngine();
        restored.LoadMap("de_current", "", saved);
        Assert.Equal(120, restored.GetStatus().TimeLeftSeconds);
        restored.SetPlayers([1]);
        f.Clock.Advance(10);
        Assert.Equal(110, restored.GetStatus().TimeLeftSeconds);
    }

    [Fact]
    public void NoMapsNeverChangesOrReloadsTheCurrentMapAfterNativeMatchEnd()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.Configure(RotationConfiguration.Empty, []);
        f.Engine.ObserveNativeMatch(new(false, true, false, 0, 0));
        f.Engine.MatchEnded();
        f.Clock.Advance(7200);
        f.Engine.Tick();
        f.Engine.RoundEnded();
        Assert.Equal(RotationPauseReason.NoMaps, f.Engine.PauseReason);
        Assert.Null(f.Engine.GetStatus().TimeLeftSeconds);
        Assert.Null(f.Engine.Vote);
        Assert.Empty(f.Changes);
    }

    [Fact]
    public void ClearingPoolWhilePostMatchChangeIsDelayedCancelsTheChange()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.MatchEnded();
        f.Engine.Configure(RotationConfiguration.Empty, []);
        f.Clock.Advance(10);
        f.Engine.Tick();
        Assert.Empty(f.Changes);
        Assert.Equal(RotationState.Playing, f.Engine.State);
    }

    [Fact]
    public void HotReloadRemembersMatchEndWhileTheVoteIsStillRunning()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.StartVote(NextMapSource.Admin);
        f.Engine.CastVote(1, f.Engine.Vote!.Id, 3);
        f.Engine.MatchEnded();
        var saved = JsonSerializer.Deserialize<RotationCheckpoint>(JsonSerializer.Serialize(f.Engine.Checkpoint()))!;
        var restored = f.NewEngine();
        var changes = new List<long>();
        restored.ChangeRequested += (map, _) => changes.Add(map.Id);
        restored.LoadMap("de_current", "", saved);
        restored.SetPlayers([1, 2, 3, 4]);
        f.Clock.Advance(20);
        restored.Tick();
        f.Clock.Advance(3);
        restored.Tick();
        Assert.Equal(3, Assert.Single(changes));
    }

    [Fact]
    public void NewMapDoesNotInheritThePreviousMatchEnd()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.MatchEnded();
        f.Engine.UnloadMap();
        f.Engine.LoadMap("de_current", "", f.Engine.Checkpoint());
        f.Engine.SetPlayers([1]);
        f.Clock.Advance(10);
        f.Engine.Tick();
        Assert.False(f.Engine.Checkpoint().NativeMatchEnded);
        Assert.Empty(f.Changes);
    }

    [Fact]
    public void NativeRestartCancelsTheOldPostMatchChange()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.MatchEnded();
        f.Engine.ObserveNativeMatch(new(false, true, false, null, null));
        f.Clock.Advance(10);
        f.Engine.RoundEnded();
        f.Engine.Tick();
        Assert.False(f.Engine.Checkpoint().NativeMatchEnded);
        Assert.Equal(RotationState.Playing, f.Engine.State);
        Assert.Empty(f.Changes);
    }
}
