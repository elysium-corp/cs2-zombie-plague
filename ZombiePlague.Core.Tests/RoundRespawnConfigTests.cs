using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Rounds.Registrator;
using Xunit;

namespace ZombiePlague.Core.Tests;

public sealed class RoundRespawnConfigTests
{
    [Fact]
    public void DefaultRespawnRounds_HaveExplicitPositiveLimit()
    {
        var config = new RoundConfig();

        var respawnRounds = config.GetAll()
            .OfType<IZombieRespawnConfig>()
            .Where(static round => round.ZombieRevived)
            .ToArray();

        Assert.NotEmpty(respawnRounds);
        Assert.All(respawnRounds, static round => Assert.True(round.ZombieRespawnLimit > 0));
    }

    [Fact]
    public void EnabledRespawn_WithZeroLimit_IsRejected()
    {
        var config = new InfectionConfig
        {
            ZombieRevived = true,
            ZombieRespawnLimit = 0
        };

        Assert.Throws<InvalidOperationException>(() => RoundRegistrator.ValidateRespawnConfig(config));
    }

    [Fact]
    public void DisabledRespawn_AllowsZeroLimit()
    {
        var config = new InfectionConfig
        {
            ZombieRevived = false,
            ZombieRespawnLimit = 0
        };

        RoundRegistrator.ValidateRespawnConfig(config);
    }
}
