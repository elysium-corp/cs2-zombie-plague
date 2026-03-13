using System;
using System.Linq;
using CS2ZombiePlague.Config;
using CS2ZombiePlague.Config.Round;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Data.Rounds.Contracts;
using CS2ZombiePlague.Utils.Extensions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;

namespace CS2ZombiePlague.Data.Rounds;

public class Nemesis(
    ISwiftlyCore core,
    RoundManager roundManager,
    ZombieManager zombieManager,
    NemesisConfig config) : BaseRound
{
    public override int Chance => config.Chance;
    public override string Name => "Немезида";

    public override void Start()
    {
        core.Event.OnEntityTakeDamage += OnEntityTakeDamage;
        
        var players = core.PlayerManager.GetAlive().ToList();
        var nemesis = players[Random.Shared.Next(0, players.Count)];

        zombieManager.SetNemesis(nemesis, config);

        foreach (var player in players)
        {
            if (!player.IsInfected())
            {
                player.SwitchTeam(Team.CT);
            }
        }

        if (config.IsMusicEnabled)
        {
            PlaySound();
        }

        core.PlayerManager.SendCenter("Немезида => " + nemesis.Name);
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