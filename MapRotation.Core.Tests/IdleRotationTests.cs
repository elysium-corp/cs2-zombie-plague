using System.Text.Json;
using MapRotation.Api;
using MapRotation.Core.Domain;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class IdleRotationTests
{
    [Fact]
    public void EmptyServerDoesNotSpendMapOrRtvTimeAndFirstPlayerResumesTheClock()
    {
        var f = new RotationEngineTests.Fixture(new() { ScheduledVoteEnabled = true });
        f.Engine.SetPlayers([]);
        var remaining = f.Engine.TimeLeft;
        var delay = f.Engine.RtvDelayRemaining;

        f.Clock.Advance(7200);
        f.Engine.Tick();
        f.Engine.RoundEnded();

        Assert.Equal(RotationState.Playing, f.Engine.State);
        Assert.Equal(RotationPauseReason.EmptyServer, f.Engine.GetStatus().PauseReason);
        Assert.Equal(remaining, f.Engine.TimeLeft);
        Assert.Equal(delay, f.Engine.RtvDelayRemaining);
        Assert.Null(f.Engine.Vote);
        Assert.Empty(f.Changes);
        Assert.Equal(f.Clock.GetUtcNow().Add(remaining), f.Engine.GetStatus().Deadline);

        f.Engine.SetPlayers([1]);
        f.Clock.Advance(10);
        f.Engine.Tick();

        Assert.Equal(RotationPauseReason.None, f.Engine.PauseReason);
        Assert.Equal(remaining - TimeSpan.FromSeconds(10), f.Engine.TimeLeft);
        Assert.Equal(delay - 10, f.Engine.RtvDelayRemaining);
    }

    [Fact]
    public void IdlePeriodPreservesTimeAlreadyPlayed()
    {
        var f = new RotationEngineTests.Fixture();
        f.Clock.Advance(600);
        f.Engine.SetPlayers([]);
        f.Clock.Advance(3600);
        f.Engine.SetPlayers([1, 2]);

        Assert.Equal(TimeSpan.FromSeconds(2100), f.Engine.TimeLeft);
        f.Clock.Advance(2100);
        f.Engine.Tick();
        Assert.Equal(RotationState.FinalRound, f.Engine.State);
        f.Engine.RoundEnded();
        Assert.Single(f.Changes);
    }

    [Fact]
    public void BotsDoNotResumeTheClockAndVotingEligibilityDoesNotHideHumanPresence()
    {
        var f = new RotationEngineTests.Fixture(new() { ExcludeBots = false, IncludeSpectators = false });
        f.Engine.SetPlayers([], eligiblePlayerCount: 10, humanPlayerCount: 0);
        f.Clock.Advance(7200);
        f.Engine.Tick();
        Assert.Equal(TimeSpan.FromSeconds(2700), f.Engine.TimeLeft);
        Assert.Equal(RotationPauseReason.EmptyServer, f.Engine.PauseReason);

        f.Engine.SetPlayers([], eligiblePlayerCount: 10, humanPlayerCount: 1);
        f.Clock.Advance(5);
        Assert.Equal(TimeSpan.FromSeconds(2695), f.Engine.TimeLeft);
        Assert.Equal(RotationReply.NotEligible, f.Engine.Rtv(1));
    }

    [Fact]
    public void EmptyCatalogPausesRotationWithoutBlockingGameRounds()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.Configure(RotationConfiguration.Empty, []);
        f.Clock.Advance(7200);
        f.Engine.Tick();
        f.Engine.RoundEnded();

        Assert.Equal(RotationPauseReason.NoMaps, f.Engine.PauseReason);
        Assert.False(f.Engine.GetStatus().RotationEnabled);
        Assert.Null(f.Engine.GetStatus().TimeLeftSeconds);
        Assert.Equal(RotationState.Playing, f.Engine.State);
        Assert.Null(f.Engine.NextMap);
        Assert.Empty(f.Changes);

        f.Engine.Configure(RotationConfiguration.Create(new(), f.Maps), [1, 2, 3, 4]);
        Assert.Equal(RotationPauseReason.None, f.Engine.PauseReason);
        Assert.True(f.Engine.GetStatus().RotationEnabled);
        Assert.Equal(TimeSpan.FromSeconds(2700), f.Engine.TimeLeft);
        Assert.NotNull(f.Engine.NextMap);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreviousFinalRoundWithoutTargetIsRecoveredFromItsCheckpoint(bool catalogNowAvailable)
    {
        var f = new RotationEngineTests.Fixture();
        var old = f.Engine.Checkpoint() with
        {
            State = RotationState.FinalRound, NextMapId = null,
            FinalRoundAt = f.Engine.Deadline, PausedAt = null
        };
        f.Clock.Advance(7200);
        var restored = new RotationEngine(f.Clock, new RotationEngineTests.RandomValue());
        restored.Configure(catalogNowAvailable ? f.Engine.Configuration : RotationConfiguration.Empty, [1, 2, 3, 4]);
        var legacy = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(old))!.AsObject();
        legacy.Remove(nameof(RotationCheckpoint.PausedAt));
        legacy.Remove(nameof(RotationCheckpoint.PausedDuration));
        restored.LoadMap("de_current", "", legacy.Deserialize<RotationCheckpoint>());
        restored.SetPlayers([1]);
        restored.Tick();

        Assert.Equal(RotationState.Playing, restored.State);
        Assert.Equal(catalogNowAvailable ? RotationPauseReason.None : RotationPauseReason.NoMaps, restored.PauseReason);
        Assert.Equal(TimeSpan.FromSeconds(2700), restored.TimeLeft);
        Assert.Null(restored.Checkpoint().FinalRoundAt);
    }

    [Fact]
    public void HotReloadRetainsPausedRemainingTimeAndResumesVoteWithItsFullRemainder()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.StartVote(NextMapSource.Admin);
        f.Clock.Advance(5);
        f.Engine.SetPlayers([]);
        var saved = JsonSerializer.Deserialize<RotationCheckpoint>(JsonSerializer.Serialize(f.Engine.Checkpoint()))!;
        f.Clock.Advance(7200);

        var restored = f.NewEngine();
        restored.LoadMap("de_current", "", saved);
        restored.Tick();
        Assert.Equal(TimeSpan.FromSeconds(2695), restored.TimeLeft);
        Assert.NotNull(restored.Vote);
        restored.SetPlayers([1, 2]);
        Assert.Equal(f.Clock.GetUtcNow().AddSeconds(15), restored.Vote!.EndsAt);
        f.Clock.Advance(14);
        restored.Tick();
        Assert.NotNull(restored.Vote);
        f.Clock.Advance(1);
        restored.Tick();
        Assert.Null(restored.Vote);
    }

    [Fact]
    public void FinalRoundTimeoutDoesNotRunWhileServerIsEmpty()
    {
        var f = new RotationEngineTests.Fixture();
        f.Clock.Advance(2700);
        f.Engine.Tick();
        f.Clock.Advance(100);
        f.Engine.SetPlayers([]);
        f.Clock.Advance(7200);
        f.Engine.Tick();
        Assert.Empty(f.Changes);

        f.Engine.SetPlayers([1]);
        f.Clock.Advance(499);
        f.Engine.Tick();
        Assert.Empty(f.Changes);
        f.Clock.Advance(1);
        f.Engine.Tick();
        Assert.True(Assert.Single(f.Changes).Forced);
    }

    [Fact]
    public void AdminCanChangeMapOnEmptyServerWithoutStartingAnEmptyVote()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.SetPlayers([]);
        Assert.False(f.Engine.StartVote(NextMapSource.Admin));
        Assert.True(f.Engine.SetNext(2, changeNow: true));
        Assert.Equal(2, Assert.Single(f.Changes).Map.Id);
    }

    [Fact]
    public void VoteOnlyMapsKeepRtvAvailableWhenScheduledVotingIsDisabled()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings,
            f.Maps.Select(map => map with { AllowAutoRotation = false })), [1, 2, 3, 4]);
        Assert.Equal(RotationPauseReason.None, f.Engine.PauseReason);
        f.Clock.Advance(300);

        Assert.Equal(0, f.Engine.RtvDelayRemaining);
        f.Engine.Rtv(1); f.Engine.Rtv(2); f.Engine.Rtv(3);
        Assert.Equal(NextMapSource.Rtv, f.Engine.Vote!.Source);
        Assert.Equal(RotationPauseReason.None, f.Engine.PauseReason);
        Assert.Equal(f.Clock.GetUtcNow().AddSeconds(20), f.Engine.Vote!.EndsAt);
        f.Clock.Advance(20);
        f.Engine.Tick();
        Assert.NotNull(f.Engine.NextMap);
    }

    [Fact]
    public void ExpiredTimerWithoutAChosenMapWaitsForVotingAndDoesNotForceChangeOnVoteCompletion()
    {
        var f = new RotationEngineTests.Fixture();
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings,
            f.Maps.Select(map => map with { AllowAutoRotation = false })), [1, 2, 3, 4]);
        f.Clock.Advance(7200);
        f.Engine.Tick();
        Assert.Equal(RotationState.Playing, f.Engine.State);
        Assert.Empty(f.Changes);

        Assert.True(f.Engine.StartVote(NextMapSource.Admin));
        f.Clock.Advance(20);
        f.Engine.Tick();
        Assert.Equal(RotationState.FinalRound, f.Engine.State);
        Assert.NotNull(f.Engine.NextMap);
        Assert.Empty(f.Changes);
        f.Engine.RoundEnded();
        Assert.False(Assert.Single(f.Changes).Forced);
    }

    [Fact]
    public void DisabledFinalTargetDoesNotLeaveAnUnchangeableFinalRound()
    {
        var f = new RotationEngineTests.Fixture();
        f.Clock.Advance(2700);
        f.Engine.Tick();
        f.Engine.Configure(RotationConfiguration.Empty, []);
        f.Engine.RoundEnded();

        Assert.Equal(RotationState.Playing, f.Engine.State);
        Assert.Equal(RotationPauseReason.NoMaps, f.Engine.PauseReason);
        Assert.Empty(f.Changes);
    }

    [Fact]
    public void VoteWithoutAnyRemainingCandidateCannotScheduleAChangeAfterCatalogRecovery()
    {
        var f = new RotationEngineTests.Fixture();
        f.Clock.Advance(2695);
        f.Engine.StartVote(NextMapSource.Rtv);
        f.Engine.Configure(RotationConfiguration.Empty, []);
        f.Clock.Advance(5);
        f.Engine.Tick();
        f.Engine.RoundEnded();

        Assert.Null(f.Engine.Vote);
        Assert.Null(f.Engine.Checkpoint().ChangeAt);
        Assert.Equal(RotationState.Playing, f.Engine.State);
        Assert.Equal(RotationPauseReason.NoMaps, f.Engine.PauseReason);

        f.Engine.Configure(RotationConfiguration.Create(new(), f.Maps), [1, 2, 3, 4]);
        f.Engine.Tick();
        Assert.Empty(f.Changes);
        Assert.Equal(TimeSpan.FromSeconds(5), f.Engine.TimeLeft);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("disabled")]
    [InlineData("uninstalled")]
    [InlineData("current-only")]
    public void UnusablePoolDisablesCommandsEvenWithoutPlayers(string pool)
    {
        var f = new RotationEngineTests.Fixture();
        IEnumerable<RotationMap> maps = pool switch
        {
            "empty" => [],
            "disabled" => f.Maps.Select(map => map with { Enabled = false }),
            "current-only" => f.Maps.Where(map => map.MapName == "de_current"),
            _ => f.Maps.AsEnumerable()
        };
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings, maps), pool == "uninstalled" ? [] : [1, 2, 3, 4]);
        Assert.Equal(RotationReply.Disabled, f.Engine.Rtv(1));
        Assert.Equal(RotationReply.Disabled, f.Engine.Nominate(1, 2));
        Assert.False(f.Engine.StartVote(NextMapSource.Admin));
        Assert.False(f.Engine.SetNext(2, changeNow: true));
        f.Engine.SetPlayers([]);
        f.Clock.Advance(86400);
        f.Engine.Tick();
        f.Engine.RoundEnded();
        Assert.False(f.Engine.RotationEnabled);
        Assert.Equal(RotationPauseReason.NoMaps, f.Engine.PauseReason);
        Assert.Empty(f.Changes);
    }

    [Fact]
    public void ClearingPoolCancelsParticipationAndRestoredVotesUntilMapsReturn()
    {
        var f = new RotationEngineTests.Fixture();
        f.Clock.Advance(300);
        f.Engine.Nominate(1, 2);
        f.Engine.Rtv(1);
        f.Engine.StartVote(NextMapSource.Admin);
        var oldVote = f.Engine.Vote!.Id;
        f.Engine.CastVote(1, oldVote, 2);
        var active = f.Engine.Checkpoint();
        List<VoteArchive> finished = [];
        f.Engine.VoteFinished += finished.Add;

        f.Engine.Configure(RotationConfiguration.Empty, []);

        Assert.Null(Assert.Single(finished).WinnerId);
        Assert.Null(f.Engine.Vote);
        Assert.Null(f.Engine.LastResult);
        Assert.Null(f.Engine.Nomination(1));
        Assert.Equal(0, f.Engine.RtvVotes);
        Assert.False(f.Engine.CastVote(1, oldVote, 2));
        Assert.Null(f.Engine.NextMap);

        var restored = new RotationEngine(f.Clock, new RotationEngineTests.RandomValue());
        restored.Configure(RotationConfiguration.Empty, []);
        restored.LoadMap("de_current", "", active);
        restored.SetPlayers([1, 2, 3, 4]);
        Assert.Null(restored.Vote);
        Assert.Equal(0, restored.RtvVotes);
        Assert.Null(restored.Nomination(1));
        f.Clock.Advance(86400);
        restored.Tick();
        Assert.Equal(RotationState.Playing, restored.State);

        restored.Configure(RotationConfiguration.Create(f.Engine.Settings, f.Maps), [1, 2, 3, 4]);
        Assert.True(restored.RotationEnabled);
        Assert.Equal(TimeSpan.FromSeconds(2400), restored.TimeLeft);
        Assert.True(restored.StartVote(NextMapSource.Admin));
        Assert.NotEqual(oldVote, restored.Vote!.Id);
    }
}
