using SwiftlyS2.Shared;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Survivor(
    ISwiftlyCore core,
    RoundManager roundManager,
    IZombieManager zombieManager,
    IHumanManager humanManager,
    SurvivorConfig config) : BaseRound(core, roundManager)
{
    public override int Chance => config.Chance;
    public override string Name => "Выживший";

    protected override void OnStart()
    {
        var allPlayers = Core.PlayerManager.GetAlive().ToList();
        var survivor = allPlayers[Random.Shared.Next(0, allPlayers.Count)];

        foreach (var player in allPlayers)
        {
            if (!player.Equals(survivor))
            {
                zombieManager.CreateZombie(player);
            }
        }

        humanManager.SetSurvivor(survivor, config);

        if (config.IsMusicEnabled)
        {
            SoundExt.PlayGlobal(config.MusicSoundName);
        }
        
        Core.PlayerManager.SendCenter("Выживший => " + survivor.Controller.PlayerName);
    }
}