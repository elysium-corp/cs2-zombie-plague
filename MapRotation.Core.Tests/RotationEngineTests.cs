using System.Collections.Immutable;
using MapRotation.Api;
using MapRotation.Core.Domain;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class RotationEngineTests
{
    [Fact]
    public void DeadlineWaitsForRoundEndAndChangeIsIdempotent()
    {
        var f = new Fixture();
        Assert.Equal("45:00", RotationCoordinator.Duration(f.Engine.TimeLeft));
        f.Clock.Advance(2700); f.Engine.Tick();
        Assert.Equal(RotationState.FinalRound, f.Engine.State); Assert.Empty(f.Changes);
        f.Engine.RoundEnded(); f.Engine.RoundEnded(); f.Engine.Tick();
        Assert.Single(f.Changes); Assert.False(f.Changes[0].Forced);
    }
    [Fact]
    public void RoundEndAtDeadlineDoesNotNeedTimerToRunFirst()
    {
        var f = new Fixture(); f.Clock.Advance(2700); f.Engine.RoundEnded(); Assert.Single(f.Changes);
    }
    [Fact]
    public void StalledFinalRoundForcesExactlyOneChange()
    {
        var f = new Fixture(); f.Clock.Advance(3300); f.Engine.Tick(); f.Engine.Tick();
        Assert.True(Assert.Single(f.Changes).Forced);
    }
    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(-1, "00:00")]
    [InlineData(757, "12:37")]
    [InlineData(3601, "60:01")]
    public void TimeLeftUsesTotalMinutes(int seconds, string expected) => Assert.Equal(expected, RotationCoordinator.Duration(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void HotReloadKeepsDeadlineAndAdminSelection()
    {
        var f = new Fixture(); f.Engine.SetNext(3, false); f.Clock.Advance(240);
        var state = f.Engine.Checkpoint(); var replacement = f.NewEngine();
        replacement.LoadMap("de_current", "", state);
        Assert.Equal(state.Deadline, replacement.Deadline); Assert.Equal(3, replacement.NextMapId);
        Assert.Equal(NextMapSource.Admin, replacement.Source);
    }
    [Fact]
    public void ActualReloadOfSameMapStartsANewTimer()
    {
        var f = new Fixture(); var before = f.Engine.Deadline; f.Clock.Advance(100);
        f.Engine.UnloadMap(); f.Engine.LoadMap("de_current", "", f.Engine.Checkpoint());
        Assert.Equal(before.AddSeconds(100), f.Engine.Deadline);
    }
    [Fact]
    public void ChangedMapDoesNotRestorePreviousDeadline()
    {
        var f = new Fixture(); f.Clock.Advance(100);
        f.Engine.LoadMap("de_second", "", f.Engine.Checkpoint());
        Assert.Equal(f.Clock.GetUtcNow().AddSeconds(2700), f.Engine.Deadline);
    }

    [Fact]
    public void RtvRequiresDelayAndMinimumPlayersAndDeduplicatesSteamId()
    {
        var f = new Fixture();
        Assert.Equal(RotationReply.Delay, f.Engine.Rtv(1));
        f.Clock.Advance(300); f.Engine.SetPlayers([1, 2, 3]);
        Assert.Equal(RotationReply.TooFewPlayers, f.Engine.Rtv(1));
        f.Engine.SetPlayers([1, 2, 3, 4]);
        Assert.Equal(RotationReply.Accepted, f.Engine.Rtv(1));
        Assert.Equal(RotationReply.Duplicate, f.Engine.Rtv(1));
        Assert.Equal(3, f.Engine.RtvRequired); Assert.Equal(1, f.Engine.RtvVotes);
    }
    [Fact]
    public void RtvRatioRoundsUpAndThresholdChangesWithConnections()
    {
        var f = new Fixture(); f.Clock.Advance(300); f.Engine.SetPlayers([1, 2, 3, 4, 5, 6]);
        Assert.Equal(4, f.Engine.RtvRequired);
        f.Engine.Rtv(1); f.Engine.Rtv(2); f.Engine.Rtv(3);
        Assert.Null(f.Engine.Vote);
        f.Engine.SetPlayers([1, 2, 3, 4, 5]);
        Assert.Equal(NextMapSource.Rtv, f.Engine.Vote!.Source);
    }
    [Fact]
    public void DisconnectRemovesRtvNominationAndBallot()
    {
        var f = new Fixture(); f.Clock.Advance(300); f.Engine.Nominate(1, 2); f.Engine.Rtv(1);
        f.Engine.StartVote(NextMapSource.ScheduledVote);
        f.Engine.CastVote(1, f.Engine.Vote!.Id, 2);
        f.Engine.SetPlayers([2, 3, 4]);
        Assert.Equal(0, f.Engine.RtvVotes); Assert.Null(f.Engine.Nomination(1)); Assert.Empty(f.Engine.Vote.Votes);
    }
    [Fact]
    public void NominationReplacesPreviousAndLeadersEnterTheVoteFirst()
    {
        var f = new Fixture(); f.Engine.Nominate(1, 2); f.Engine.Nominate(1, 3); f.Engine.Nominate(2, 3);
        f.Engine.StartVote(NextMapSource.Admin);
        Assert.Equal(3, f.Engine.Nomination(1)); Assert.Equal(3, f.Engine.Vote!.Options[0].Id);
        Assert.Equal(f.Engine.Vote.Options.Length, f.Engine.Vote.Options.Select(map => map.Id).Distinct().Count());
    }
    [Fact]
    public void RtvWinnerMakesCurrentRoundFinal()
    {
        var f = new Fixture(); f.Clock.Advance(300); f.Engine.Rtv(1); f.Engine.Rtv(2); f.Engine.Rtv(3);
        var vote = f.Engine.Vote!; f.Engine.CastVote(1, vote.Id, 3);
        f.Clock.Advance(20); f.Engine.Tick();
        Assert.Equal(RotationState.FinalRound, f.Engine.State); Assert.Equal(3, f.Engine.NextMapId); Assert.Empty(f.Changes);
        f.Engine.RoundEnded(); Assert.Equal(3, Assert.Single(f.Changes).Map.Id);
    }
    [Fact]
    public void DelayedRtvChangeDoesNotChangeBeforeDelayExpires()
    {
        var f = new Fixture(new() { RtvChangeDelaySeconds = 5 }); f.Clock.Advance(300);
        f.Engine.StartVote(NextMapSource.Rtv); f.Clock.Advance(20); f.Engine.Tick(); f.Engine.RoundEnded();
        Assert.Empty(f.Changes); f.Clock.Advance(4); f.Engine.Tick(); Assert.Empty(f.Changes);
        f.Clock.Advance(1); f.Engine.Tick(); Assert.Single(f.Changes);
    }
    [Fact]
    public void ImmediateRtvStillHonorsConfiguredDelay()
    {
        var f = new Fixture(new() { RtvChangeMode = "immediate", RtvChangeDelaySeconds = 5 });
        f.Engine.StartVote(NextMapSource.Rtv); f.Clock.Advance(20); f.Engine.Tick(); Assert.Empty(f.Changes);
        f.Clock.Advance(5); f.Engine.Tick(); Assert.Single(f.Changes);
    }
    [Fact]
    public void ScheduledVoteRunsOnceAndRetainsFullDurationNearDeadline()
    {
        var f = new Fixture(new() { ScheduledVoteEnabled = true });
        f.Clock.Advance(2400); f.Engine.Tick(); var vote = f.Engine.Vote!;
        f.Engine.Tick(); Assert.Equal(vote.Id, f.Engine.Vote!.Id);
        f.Clock.Advance(20); f.Engine.Tick();
        Assert.Equal(RotationState.NextMapSelected, f.Engine.State); f.Engine.Tick(); Assert.Null(f.Engine.Vote);
    }
    [Fact]
    public void AdminSelectionCancelsVoteAndProtectsItFromScheduledVote()
    {
        var f = new Fixture(new() { ScheduledVoteEnabled = true }); f.Engine.StartVote(NextMapSource.Rtv);
        var obsolete = f.Engine.Vote!.Id; f.Engine.SetNext(3, false);
        Assert.False(f.Engine.CastVote(1, obsolete, 2));
        f.Clock.Advance(2401); f.Engine.Tick(); Assert.Null(f.Engine.Vote); Assert.Equal(3, f.Engine.NextMapId);
    }
    [Fact]
    public void BallotCanBeReplacedButNotDuplicatedOrAppliedToAnotherVote()
    {
        var f = new Fixture(); f.Engine.StartVote(NextMapSource.Admin); var id = f.Engine.Vote!.Id;
        Assert.False(f.Engine.CastVote(1, Guid.NewGuid(), 2)); Assert.False(f.Engine.CastVote(99, id, 2));
        Assert.True(f.Engine.CastVote(1, id, 2)); Assert.True(f.Engine.CastVote(1, id, 3));
        Assert.Equal(3, Assert.Single(f.Engine.Vote.Votes).Value);
        f.Clock.Advance(20); Assert.False(f.Engine.CastVote(1, id, 2));
    }
    [Fact]
    public void ZeroVotesUseProvisionalMapAndTieOnlyChoosesLeaders()
    {
        var f = new Fixture(); var provisional = f.Engine.NextMapId; f.Engine.StartVote(NextMapSource.Admin);
        f.Clock.Advance(20); f.Engine.Tick(); Assert.Equal(provisional, f.Engine.NextMapId);
        var g = new Fixture(); g.Engine.StartVote(NextMapSource.Admin); var vote = g.Engine.Vote!;
        g.Engine.CastVote(1, vote.Id, 2); g.Engine.CastVote(2, vote.Id, 3);
        g.Clock.Advance(20); g.Engine.Tick(); Assert.Contains(g.Engine.NextMapId, new long?[] { 2, 3 });
    }
    [Fact]
    public void DeadlineDuringVoteFinishesTheVoteBeforeChangingOnRoundEnd()
    {
        var f = new Fixture(); f.Clock.Advance(2695); f.Engine.StartVote(NextMapSource.Rtv);
        f.Engine.CastVote(1, f.Engine.Vote!.Id, 3); f.Clock.Advance(5); f.Engine.Tick();
        Assert.NotNull(f.Engine.Vote); f.Engine.RoundEnded();
        Assert.Equal(3, Assert.Single(f.Changes).Map.Id); Assert.Null(f.Engine.Vote);
    }
    [Fact]
    public void VoteOptionsStayFrozenAcrossCatalogRefreshButDisabledWinnerIsRejected()
    {
        var f = new Fixture(); f.Engine.StartVote(NextMapSource.Admin); var original = f.Engine.Vote!;
        f.Engine.CastVote(1, original.Id, 3);
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings, f.Maps.Select(map => map with { Enabled = map.Id != 3 })), [1, 2, 3, 4]);
        Assert.Equal(original.Options, f.Engine.Vote!.Options);
        f.Clock.Advance(20); f.Engine.Tick(); Assert.NotEqual(3, f.Engine.NextMapId);
    }
    [Fact]
    public void FailedChangeRetriesAnotherValidatedMap()
    {
        var f = new Fixture(); f.Engine.SetNext(2, true); f.Engine.ChangeFailed(2); f.Engine.Tick();
        Assert.Single(f.Changes); f.Clock.Advance(15); f.Engine.Tick();
        Assert.Equal(2, f.Changes.Count); Assert.NotEqual(2, f.Changes[1].Map.Id);
    }
    [Fact]
    public void HistoryIsRecordedOnlyAtMapUnload()
    {
        var f = new Fixture(); List<MapHistoryEntry> history = []; f.Engine.MapFinished += history.Add;
        f.Engine.SetNext(2, true); Assert.Empty(history); f.Engine.UnloadMap(); f.Engine.UnloadMap(); Assert.Single(history);
        Assert.Equal(1, f.Engine.Checkpoint().History[0]);
    }
    [Fact]
    public void OptionalBotPresenceAffectsThresholdWithoutCreatingFakeBallots()
    {
        var f = new Fixture(); f.Engine.SetPlayers([1, 2, 3, 4], eligiblePlayerCount: 10);
        Assert.Equal(6, f.Engine.RtvRequired);
        f.Clock.Advance(300); Assert.Equal(RotationReply.NotEligible, f.Engine.Rtv(0));
    }
    [Fact]
    public void ParticipationUsesUniqueEligibleHumansAndFreezesAtVoteCompletion()
    {
        var f = new Fixture();
        f.Engine.SetPlayers([1, 1, 2, 3, 4], eligiblePlayerCount: 10);
        Assert.Equal(4, f.Engine.EligibleVoterCount);
        Assert.True(f.Engine.StartVote(NextMapSource.Admin));
        var vote = f.Engine.Vote!;
        f.Engine.CastVote(1, vote.Id, 2);
        f.Engine.CastVote(1, vote.Id, 3);
        f.Engine.CastVote(2, vote.Id, 3);
        Assert.Equal(2, f.Engine.Vote!.Votes.Count);
        f.Clock.Advance(20); f.Engine.Tick();
        var result = f.Engine.LastResult!;
        f.Engine.SetPlayers([1]);
        Assert.Equal(4, result.EligibleVoterCount);
        Assert.Equal(2, result.Vote.Votes.Count);
        Assert.Equal(1, f.Engine.EligibleVoterCount);
    }
    [Fact]
    public void LegacyCandidateSettingsCannotProduceMoreThanSixOptions()
    {
        var maps = Enumerable.Range(1, 12).Select(id => new RotationMap { Id = id, Weight = 1 }).ToImmutableArray();
        var nominations = Enumerable.Range(1, 12).ToDictionary(id => (ulong)id, id => (long)id);
        var candidates = new MapCandidates(new RandomValue());
        var options = candidates.VoteOptions(maps, nominations, new() { VoteOptionsCount = 30, NominationSlots = 12 });
        Assert.Equal(6, options.Length);
        Assert.Equal(6, options.Select(map => map.Id).Distinct().Count());
    }
    [Fact]
    public void LegacyCheckpointRemovesCandidatesAndBallotsOutsideSixSlots()
    {
        var f = new Fixture();
        f.Engine.StartVote(NextMapSource.Admin);
        var checkpoint = f.Engine.Checkpoint();
        var options = Enumerable.Range(1, 9).Select(id => f.Maps[1] with { Id = id }).ToImmutableArray();
        checkpoint = checkpoint with { Vote = checkpoint.Vote! with
        {
            Options = options, Votes = new Dictionary<ulong, long> { [1] = 2, [2] = 9 }.ToImmutableDictionary()
        } };
        var restored = f.NewEngine();
        restored.LoadMap("de_current", "", checkpoint);
        Assert.Equal(6, restored.Vote!.Options.Length);
        Assert.Equal(2, Assert.Single(restored.Vote.Votes).Value);
        Assert.Equal(checkpoint.Vote.Id, restored.Vote.Id);
        Assert.Equal(checkpoint.Vote.EndsAt, restored.Vote.EndsAt);
    }
    [Fact]
    public void ConfigRefreshPreservesTheCurrentDeadline()
    {
        var f = new Fixture(); var deadline = f.Engine.Deadline;
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings with { MapDurationSeconds = 5000 }, f.Maps), [1, 2, 3, 4]);
        Assert.Equal(deadline, f.Engine.Deadline);
    }
    [Fact]
    public void HotReloadResumesVoteWithOriginalCandidatesAndDeadline()
    {
        var f = new Fixture(); f.Engine.StartVote(NextMapSource.Rtv); f.Engine.CastVote(1, f.Engine.Vote!.Id, 2);
        var saved = f.Engine.Checkpoint(); var restored = f.NewEngine(); restored.LoadMap("de_current", "", saved);
        restored.SetPlayers([1, 2, 3, 4]);
        Assert.Equal(saved.Vote, restored.Vote);
        f.Clock.Advance(20); restored.Tick(); Assert.Equal(2, restored.NextMapId);
    }
    [Fact]
    public void InvalidCurrentAndDisabledMapsCannotBeAdminTargets()
    {
        var f = new Fixture(); Assert.False(f.Engine.SetNext(1, true)); Assert.False(f.Engine.SetNext(55, true));
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings, f.Maps), [1, 3]);
        Assert.False(f.Engine.SetNext(2, true)); Assert.Empty(f.Changes);
    }

    internal sealed class Clock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(double seconds) => _now = _now.AddSeconds(seconds);
    }
    internal sealed class RandomValue(double value = 0) : IRotationRandom { public double NextDouble() => value; }
    internal sealed class Fixture
    {
        public Clock Clock { get; } = new();
        public RotationMap[] Maps { get; } = Enumerable.Range(1, 4).Select(id => new RotationMap
            { Id = id, Key = "map" + id, MapName = id == 1 ? "de_current" : "de_map" + id, DisplayName = "Map " + id }).ToArray();
        public RotationEngine Engine { get; }
        public List<(RotationMap Map, bool Forced)> Changes { get; } = [];
        public Fixture(RotationSettings? settings = null)
        {
            Engine = new(Clock, new RandomValue());
            Engine.Configure(RotationConfiguration.Create(settings ?? new() { ScheduledVoteEnabled = false }, Maps), [1, 2, 3, 4]);
            Engine.LoadMap("de_current", ""); Engine.SetPlayers([1, 2, 3, 4]);
            Engine.ChangeRequested += (map, forced) => Changes.Add((map, forced));
        }
        public RotationEngine NewEngine()
        {
            var engine = new RotationEngine(Clock, new RandomValue());
            engine.Configure(Engine.Configuration, Maps.Select(map => map.Id)); return engine;
        }
    }
}
