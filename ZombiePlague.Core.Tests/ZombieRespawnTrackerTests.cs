using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Rounds.Respawn;
using Xunit;

namespace ZombiePlague.Core.Tests;

public sealed class ZombieRespawnTrackerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RespawnLimit_AllowsConfiguredCountAndBlocksNextDeath()
    {
        var config = CreateConfig(limit: 3);
        var tracker = new ZombieRespawnTracker(config);
        const ulong steamId = 76561198000000001;

        for (var i = 1; i <= 3; i++)
        {
            Assert.True(tracker.TryQueueRespawn(steamId, Now));
            Assert.True(tracker.CompleteAutomaticRespawn(steamId));
            Assert.Equal(i, tracker.GetUsedRespawns(steamId));
        }

        Assert.False(tracker.TryQueueRespawn(steamId, Now));
        Assert.True(tracker.IsEliminated(steamId));
        Assert.Equal(3, tracker.GetUsedRespawns(steamId));
    }

    [Fact]
    public void ReconnectWithSameSteamId_DoesNotResetCounter()
    {
        var tracker = new ZombieRespawnTracker(CreateConfig(limit: 3));
        const ulong steamId = 76561198000000002;

        Assert.True(tracker.TryQueueRespawn(steamId, Now));
        Assert.True(tracker.CompleteAutomaticRespawn(steamId));
        Assert.True(tracker.TryQueueRespawn(steamId, Now));
        Assert.True(tracker.CompleteAutomaticRespawn(steamId));

        // Новый IPlayer/SessionId здесь не участвует: состояние привязано к SteamID.
        Assert.Equal(2, tracker.GetUsedRespawns(steamId));
        Assert.True(tracker.TryQueueRespawn(steamId, Now));
        Assert.True(tracker.CompleteAutomaticRespawn(steamId));
        Assert.Equal(3, tracker.GetUsedRespawns(steamId));
    }

    [Fact]
    public void NewRound_NewTrackerStartsFromZero()
    {
        var config = CreateConfig(limit: 1);
        const ulong steamId = 76561198000000003;

        var previousRound = new ZombieRespawnTracker(config);
        Assert.True(previousRound.TryQueueRespawn(steamId, Now));
        Assert.True(previousRound.CompleteAutomaticRespawn(steamId));
        Assert.False(previousRound.TryQueueRespawn(steamId, Now));

        var nextRound = new ZombieRespawnTracker(config);

        Assert.Equal(0, nextRound.GetUsedRespawns(steamId));
        Assert.False(nextRound.IsEliminated(steamId));
        Assert.True(nextRound.TryQueueRespawn(steamId, Now));
    }

    [Fact]
    public void PendingRespawn_PreservesOriginalDeadlineAcrossReconnect()
    {
        var tracker = new ZombieRespawnTracker(CreateConfig(limit: 3, delaySeconds: 5));
        const ulong steamId = 76561198000000004;

        Assert.True(tracker.TryQueueRespawn(steamId, Now));

        var remaining = tracker.GetRemainingDelay(steamId, Now.AddSeconds(2));

        Assert.Equal(TimeSpan.FromSeconds(3), remaining);
        Assert.True(tracker.IsPending(steamId));
        Assert.Equal(0, tracker.GetUsedRespawns(steamId));
    }

    [Fact]
    public void ExternalRespawn_DoesNotConsumeAutomaticLife()
    {
        var tracker = new ZombieRespawnTracker(CreateConfig(limit: 3));
        const ulong steamId = 76561198000000005;

        Assert.True(tracker.TryQueueRespawn(steamId, Now));

        tracker.CompleteExternalRespawn(steamId);

        Assert.False(tracker.IsPending(steamId));
        Assert.Equal(0, tracker.GetUsedRespawns(steamId));
        Assert.True(tracker.TryQueueRespawn(steamId, Now));
    }

    [Fact]
    public void DisabledRespawn_EliminatesPlayerOnDeath()
    {
        var config = CreateConfig(limit: 3);
        config.ZombieRevived = false;
        var tracker = new ZombieRespawnTracker(config);
        const ulong steamId = 76561198000000006;

        Assert.False(tracker.TryQueueRespawn(steamId, Now));
        Assert.True(tracker.IsEliminated(steamId));
        Assert.Equal(0, tracker.GetUsedRespawns(steamId));
    }

    private static InfectionConfig CreateConfig(int limit, float delaySeconds = 5)
    {
        return new InfectionConfig
        {
            ZombieRevived = true,
            ZombieRespawnLimit = limit,
            ZombieSpawnTime = delaySeconds
        };
    }
}
