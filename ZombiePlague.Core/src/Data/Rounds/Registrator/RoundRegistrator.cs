using Microsoft.Extensions.Options;
using ZombiePlague.Core.Config.Round;

namespace ZombiePlague.Core.Data.Rounds.Registrator;

internal class RoundRegistrator(IOptions<RoundConfig> config) : IRoundRegistrator
{
    private readonly List<IRoundConfig> _rounds = [];

    public IEnumerable<IRoundConfig> GetAll()
    {
        return _rounds;
    }

    public IEnumerable<IRoundConfig> GetAllEnabled()
    {
        return _rounds.Where(IsRoundEnable).ToList();
    }
    
    public void Register()
    {
        _rounds.Clear();

        var rounds = config.Value.GetAll().ToList();

        foreach (var round in rounds)
        {
            ValidateRespawnConfig(round);
        }
        
        _rounds.AddRange(rounds);
    }

    private static bool IsRoundEnable(IRoundConfig roundConfig)
    {
        return roundConfig is { Enable: true, Weight: > 0 };
    }

    internal static void ValidateRespawnConfig(IRoundConfig roundConfig)
    {
        if (roundConfig is not IZombieRespawnConfig respawnConfig)
        {
            return;
        }

        if (respawnConfig.ZombieSpawnTime < 0)
        {
            throw new InvalidOperationException(
                $"Round '{roundConfig.Name}' has negative {nameof(respawnConfig.ZombieSpawnTime)}."
            );
        }

        if (respawnConfig.ZombieRevived && respawnConfig.ZombieRespawnLimit <= 0)
        {
            throw new InvalidOperationException(
                $"Round '{roundConfig.Name}' must define a positive {nameof(respawnConfig.ZombieRespawnLimit)} when zombie respawn is enabled."
            );
        }
    }
}
