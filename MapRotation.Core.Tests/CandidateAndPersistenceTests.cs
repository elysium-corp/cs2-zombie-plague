using System.Collections.Immutable;
using System.Text.Json;
using MapRotation.Core.Database;
using MapRotation.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class CandidateAndPersistenceTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(.1, 2)]
    [InlineData(.999, 2)]
    public void WeightedSelectionRespectsTheCumulativeWeights(double random, long expected)
    {
        var maps = new RotationEngineTests.Fixture().Maps.Take(2).ToArray();
        maps[1] = maps[1] with { Weight = 9 };
        Assert.Equal(expected, new MapCandidates(new RotationEngineTests.RandomValue(random)).Weighted(maps)!.Id);
    }
    [Fact]
    public void CooldownsRelaxIndividualBeforeGlobalAndNeverAllowCurrentOrDisabled()
    {
        var f = new RotationEngineTests.Fixture();
        var maps = f.Maps.Select(map => map with { CooldownMaps = map.Id == 3 ? 5 : 0, Enabled = map.Id != 4 }).ToArray();
        var config = RotationConfiguration.Create(new() { RecentMapsExcluded = 1 }, maps);
        var selector = new MapCandidates(new RotationEngineTests.RandomValue());
        var eligible = selector.Eligible(config, "de_current", "", [2, 3], new HashSet<long> { 1, 2, 3, 4 }, _ => true, 1);
        Assert.Equal(3, Assert.Single(eligible).Id);
        var relaxed = selector.Eligible(config, "de_current", "", [2, 3], new HashSet<long> { 1, 2, 3, 4 }, _ => true, 6);
        Assert.Equal(new long[] { 2, 3 }, relaxed.Select(map => map.Id).Order().ToArray());
    }
    [Fact]
    public void WorkshopCurrentMapMatchesItsIdOrBasename()
    {
        var map = new RotationMap { Id = 1, Key = "gorodok", DisplayName = "Gorodok", MapName = "zm_gorodok_cs2_v1", WorkshopId = 3100743780 };
        Assert.True(map.IsCurrent("workshop/3100743780/zm_gorodok_cs2_v1", ""));
        Assert.True(map.IsCurrent("other", "3100743780")); Assert.True(map.IsSafe);
        Assert.Equal("host_workshop_map 3100743780", MapEngineAdapter.Command(map));
    }
    [Theory]
    [InlineData("de_mirage;quit")]
    [InlineData("de_mirage\nquit")]
    [InlineData("../de_mirage")]
    [InlineData("/de_mirage")]
    [InlineData("de_mirage\" quit")]
    public void UnsafeMapCannotReachEngineCommand(string name)
    {
        var map = new RotationEngineTests.Fixture().Maps[1] with { MapName = name };
        Assert.False(map.IsSafe); Assert.Throws<ArgumentException>(() => MapEngineAdapter.Command(map));
    }
    [Fact]
    public void StockMapUsesEngineChangelevel()
        => Assert.Equal("changelevel de_map2", MapEngineAdapter.Command(new RotationEngineTests.Fixture().Maps[1]));

    [Fact]
    public void ExplicitFallbackRescuesEmptyAutoPoolButCannotBeCurrentDisabledOrInvalid()
    {
        var f = new RotationEngineTests.Fixture();
        var maps = f.Maps.Select(map => map with { AllowAutoRotation = false }).ToArray();
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings with { FallbackMapId = 3 }, maps), [1, 2, 3, 4]);
        Assert.Equal(3, f.Engine.NextMapId);
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings, maps), [1, 2, 4]); Assert.Null(f.Engine.NextMapId);
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings with { FallbackMapId = 1 }, maps), [1, 2, 3, 4]); Assert.Null(f.Engine.NextMapId);
        f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings with { FallbackMapId = 3 }, maps.Select(map => map with { Enabled = map.Id != 3 })), [1, 2, 3, 4]);
        Assert.Null(f.Engine.NextMapId);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidWeightsCannotReplaceTheCurrentSnapshot(double weight)
    {
        var f = new RotationEngineTests.Fixture(); var snapshot = f.Engine.Configuration;
        Assert.Throws<ArgumentException>(() => f.Engine.Configure(RotationConfiguration.Create(f.Engine.Settings,
            f.Maps.Select(map => map with { Weight = weight })), [1, 2]));
        Assert.Same(snapshot, f.Engine.Configuration);
    }
    [Fact]
    public void DuplicateEngineTargetsAreRejected()
    {
        var f = new RotationEngineTests.Fixture();
        Assert.Throws<ArgumentException>(() => RotationConfiguration.Create(new(), f.Maps.Select(map => map with { MapName = "same" })));
    }
    [Fact]
    public void SerializedCheckpointRestoresTimerAndVotes()
    {
        var f = new RotationEngineTests.Fixture(); f.Engine.StartVote(Api.NextMapSource.Rtv);
        f.Engine.CastVote(1, f.Engine.Vote!.Id, 2);
        var saved = JsonSerializer.Deserialize<RotationCheckpoint>(JsonSerializer.Serialize(f.Engine.Checkpoint()))!;
        var next = f.NewEngine(); next.LoadMap("de_current", "", saved); next.SetPlayers([1, 2, 3, 4]);
        Assert.Equal(f.Engine.Deadline, next.Deadline); Assert.Equal(2, next.Vote!.Votes[1]);
    }
    [Fact]
    public void PostgreSqlModelContainsTheSixRequiredTables()
    {
        using var context = new MapRotationDbContext(new DbContextOptionsBuilder<MapRotationDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test").Options);
        var script = context.Database.GenerateCreateScript();
        foreach (var table in new[] { "settings", "maps", "runtime_state", "map_history", "vote_sessions", "vote_options" })
            Assert.Contains("map_rotation." + table, script);
        Assert.DoesNotContain("min_players", script.Replace("rtv_min_players", ""));
        Assert.DoesNotContain("max_players", script);
    }
    [Fact]
    public async Task DatabaseOutageKeepsTheLastAtomicLocalSnapshot()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var f = new RotationEngineTests.Fixture();
            var request = new SaveRequest(f.Engine.Configuration, f.Engine.Checkpoint());
            await File.WriteAllTextAsync(Path.Combine(directory, "rotation-state.json"), JsonSerializer.Serialize(new LocalRotation(request, [], [])));
            using var store = new RotationStore(new UnavailableDatabase(), NullLogger<RotationStore>.Instance, directory);
            store.Start();
            for (var attempt = 0; !store.Initialized && attempt < 100; attempt++) await Task.Delay(10);
            Assert.True(store.Initialized); Assert.Equal(f.Engine.Deadline, store.Initial!.Checkpoint!.Deadline);
            Assert.Equal(f.Maps.Length, store.Initial.Configuration.Maps.Length);
            Assert.Equal("LocalSnapshot", store.Diagnostics.Source);
            Assert.Equal("map_rotation", store.Diagnostics.ConnectionName);
            Assert.Equal("IOException", store.Diagnostics.LastErrorType);
            Assert.NotNull(store.Diagnostics.LastAttemptAtUtc);
            Assert.Null(store.Diagnostics.LastSuccessAtUtc);
            store.RequestReload(); Assert.Null(store.TakeConfiguration());
        }
        finally { Directory.Delete(directory, true); }
    }
    private sealed class UnavailableDatabase : IDbContextFactory<MapRotationDbContext>
    {
        public MapRotationDbContext CreateDbContext() => throw new IOException("offline");
    }
}
