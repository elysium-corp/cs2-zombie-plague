using Localization.Api;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data.Rounds;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Infection(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    InfectionConfig config,
    IOptions<ZombiePlagueCoreConfig> coreConfig,
    Func<ILocalizationApi> localization
) : InfectionBase(core, playerManager, coreConfig, localization, config)
{
    public override string Id => RoundIds.Infection;
    
    public override string Name => config.Name;
    
    protected override bool OnStart()
    {
        var humans = PlayerManager.GetAllAliveHumans().ToArray();

        var zombies = PlayerManager.GetAllAliveZombies().ToArray();

        if (zombies.Length > 0)
        {
            var zombie = zombies[Random.Shared.Next(zombies.Length)];

            return SetFirstZombie(zombie);
        }

        if (humans.Length == 0)
        {
            return false;
        }

        var candidate =
            humans[Random.Shared.Next(humans.Length)];

        return SetFirstZombie(candidate);
    }
    
    protected override void OnEnd()
    {
        ClearRespawns();
        PlayWinnerSound();
    }
    
    public override bool CanStart()
    {
        var humansCount = PlayerManager.GetAllAliveHumans().Count();
        
        return humansCount > 1;
    }
    
    private bool SetFirstZombie(IPlayer player)
    {
        if (!PlayerManager.IsZombie(player) && !PlayerManager.TryInfect(player))
        {
            return false;
        }

        if (!PlayerManager.TryGetZombie(player, out var firstZombie))
        {
            return false;
        }

        var health = (int)Math.Round(
            firstZombie.ZClass.Health *
            config.FirstZombieHealthRatio
        );

        player.SetHealth(health);

        if (!config.FirstZombieLeap)
        {
            var leap = firstZombie.ZClass.Abilities
                .OfType<Leap>()
                .FirstOrDefault();

            leap?.UnHook();
        }

        SoundExt.PlayAt(player, config.MusicSoundName, 1.5f);

        BroadcastLocalized(
            "ZombiePlague.Round.Infection.FirstInfected",
            new Dictionary<string, string> { ["player"] = player.Name });

        return true;
    }
}
