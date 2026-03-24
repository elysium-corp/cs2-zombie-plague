using System;
using System.Linq;
using ZPCore.Config;
using ZPCore.Config.Round;
using ZPCore.Data.Managers;
using ZPCore.Data.Rounds.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Sounds;

namespace ZPCore.Data.Rounds;

internal class Survivor(
    ISwiftlyCore core,
    RoundManager roundManager,
    ZombieManager zombieManager,
    HumanManager humanManager,
    SurvivorConfig config) : BaseRound
{
    public override int Chance => config.Chance;
    public override string Name => "Выживший";

    public override void Start()
    {
        var allPlayers = core.PlayerManager.GetAlive().ToList();
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
            PlaySound();
        }
        
        core.PlayerManager.SendCenter("Выживший => " + survivor.Controller.PlayerName);
    }

    public override void End()
    {
        roundManager.SetRound(new None());

        core.PlayerManager.SendCenter("Раунд окончен");
    }

    private void PlaySound()
    {
        using var sound = new SoundEvent(config.MusicSoundName);

        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = -1;
        sound.Volume = 0.5f;

        sound.Emit();
    }
}