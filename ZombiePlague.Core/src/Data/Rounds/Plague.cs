using Localization.Api;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Api.Data.Rounds;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds.Contracts;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Plague(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    PlagueConfig config,
    IOptions<ZombiePlagueCoreConfig> coreConfig,
    Func<ILocalizationApi> localization
) : InfectionBase(core, playerManager, coreConfig, localization, config)
{
    public override string Id => RoundIds.Plague;
    
    public override string Name => config.Name;
    
    protected override bool OnStart()
    {
        var humans = PlayerManager
                .GetAllAliveHumans()
                .ToArray();

        if (humans.Length < 2)
        {
            return false;
        }

        var infectionRatio = config.ZombieSpawnRatio;

        if (infectionRatio is <= 0.0f or >= 1.0f)
        {
            throw new InvalidOperationException($"{nameof(config.ZombieSpawnRatio)} must be between 0 and 1!");
        }

        var targetInfectedCount = Math.Clamp(
            (int)Math.Ceiling(humans.Length * infectionRatio),
            min: 1,
            max: humans.Length - 1
        );

        Random.Shared.Shuffle(humans);

        var successfulInfections = 0;

        foreach (var human in humans)
        {
            if (!PlayerManager.TryInfect(human))
            {
                continue;
            }

            successfulInfections++;

            if (successfulInfections >= targetInfectedCount)
            {
                break;
            }
        }

        if (successfulInfections < targetInfectedCount)
        {
            return false;
        }

        if (config.IsMusicEnabled && !string.IsNullOrWhiteSpace(config.MusicSoundName))
        {
            SoundExt.PlayGlobal(config.MusicSoundName);
        }

        return true;
    }
    
    protected override void OnEnd()
    {
        ClearRespawns();
        PlayWinnerSound();
    }
    
    public override bool CanStart()
    {
        var humansCount = PlayerManager.GetAllAliveHumans().Count();
        
        return humansCount >= config.MinimumHumansRequired;
    }
}
