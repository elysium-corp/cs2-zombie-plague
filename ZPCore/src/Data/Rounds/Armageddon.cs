using System.Linq;
using ZPCore.Config;
using ZPCore.Config.Round;
using ZPCore.Data.Extensions;
using ZPCore.Data.Managers;
using ZPCore.Data.Rounds.Contracts;
using ZPCore.Utils.Extensions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Sounds;

namespace ZPCore.Data.Rounds;

internal class Armageddon(
    ISwiftlyCore core,
    RoundManager roundManager,
    ZombieManager zombieManager,
    HumanManager humanManager,
    ArmageddonConfig config) : BaseRound
{
    public override int Chance => config.Chance;
    public override string Name => "Армагеддон";

    public override void Start()
    {
        core.Event.OnEntityTakeDamage += OnEntityTakeDamage;
        
        var allPlayers = core.PlayerManager.GetAlive().Shuffle().ToList();
        var countPlayers = allPlayers.Count;

        for (int order = 0; order < countPlayers; order++)
        {
            if (order < countPlayers / 2)
            {
                humanManager.SetSurvivor(allPlayers[order], config);
            }
            else
            {
                zombieManager.SetNemesis(allPlayers[order], config);
            }
        }

        if (config.IsMusicEnabled)
        {
            PlaySound();
        }

        core.PlayerManager.SendCenter(Name);
    }

    public override void End()
    {
        core.Event.OnEntityTakeDamage -= OnEntityTakeDamage;
        
        roundManager.SetRound(new None());

        core.PlayerManager.SendCenter("Раунд окончен");
    }
    
    private void OnEntityTakeDamage(IOnEntityTakeDamageEvent @event)
    {
        var attacker = @event.Info.Attacker.ResolvePlayerFromHandle();
        var victim = @event.Entity.Address.FindPlayerByPawnAddress();

        if (attacker == null || victim == null || victim.PlayerPawn == null)
        {
            return;
        }

        if (attacker.IsNemesis())
        {
            @event.Info.Damage += config.NemesisExtraDamage;
        }
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