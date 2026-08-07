using SwiftlyS2.Shared;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Survivor(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    IEventPublisher eventPublisher,
    SurvivorConfig config
) : RoundBase(core, playerManager, eventPublisher)
{
    public override string Name => config.Name;
    
    protected override void OnStart()
    {
        var humans = PlayerManager.GetAllAliveHumans().ToList();
        var selectedHuman = humans[Random.Shared.Next(humans.Count)];
        
        if (!PlayerManager.TrySetSurvivor(selectedHuman, out var survivor))
        {
            return;
        }
        
        humans.Remove(selectedHuman);

        foreach (var human in humans)
        {
            PlayerManager.TryInfect(human);
        }
        
        var health = survivor.HClass.Health + config.SurvivorBonusHealthPerZombie * humans.Count;

        survivor.HClass.Health = Math.Clamp(health, survivor.HClass.Health, int.MaxValue);
        
        if (config.IsMusicEnabled && !string.IsNullOrWhiteSpace(config.MusicSoundName))
        {
            SoundExt.PlayGlobal(config.MusicSoundName);
        }

        Core.PlayerManager.SendCenter($"Выживший => {selectedHuman.Name}");
    }

    protected override void OnEnd() { }
    
    public override bool CanStart()
    {
        var humansCount = PlayerManager.GetAllAliveHumans().Count();
        
        return humansCount > config.MinimumHumansRequired;
    }
}