using Common.Hooks.Abstractions;
using Localization.Api;
using SwiftlyS2.Shared;
using ZombiePlague.Api.Data.Rounds;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Survivor(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    SurvivorConfig config,
    Func<ILocalizationApi> localization
) : RoundBase(core, playerManager, localization)
{
    public override string Id => RoundIds.Survivor;
    
    public override string Name => config.Name;
    
    protected override bool OnStart()
    {
        var humans = PlayerManager.GetAllAliveHumans().ToList();

        if (humans.Count == 0)
        {
            return false;
        }

        var selectedHuman = humans[Random.Shared.Next(humans.Count)];

        humans.Remove(selectedHuman);

        foreach (var human in humans)
        {
            if (!PlayerManager.TryInfect(human))
            {
                return false;
            }
        }

        if (!PlayerManager.TrySetSurvivor(selectedHuman, out var survivor))
        {
            return false;
        }

        var health = survivor.HClass.Health + config.SurvivorBonusHealthPerZombie * humans.Count;

        survivor.HClass.Health = Math.Clamp(
            health,
            survivor.HClass.Health,
            int.MaxValue
        );

        if (config.IsMusicEnabled && !string.IsNullOrWhiteSpace(config.MusicSoundName))
        {
            SoundExt.PlayGlobal(config.MusicSoundName);
        }

        BroadcastLocalized(
            "ZombiePlague.Round.Survivor.Selected",
            new Dictionary<string, string> { ["player"] = selectedHuman.Name });

        return true;
    }

    protected override void OnEnd() { }
    
    public override bool CanStart()
    {
        var humansCount = PlayerManager.GetAllAliveHumans().Count();
        
        return humansCount >= config.MinimumHumansRequired;
    }
}
