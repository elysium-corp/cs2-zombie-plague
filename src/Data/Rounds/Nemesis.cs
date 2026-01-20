using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;

namespace CS2ZombiePlague.Data.Rounds;

public class Nemesis(
    ISwiftlyCore core,
    RoundManager roundManager,
    ZombieManager zombieManager,
    NemesisConfig config) : IRound
{
    public void Start()
    {
        var players = core.PlayerManager.GetAlive().ToList();
        var nemesis = players[Random.Shared.Next(0, players.Count)];

        zombieManager.CreateNemesis(nemesis);
        Initialize(nemesis);

        foreach (var player in players)
        {
            if (!player.IsInfected())
            {
                player.SwitchTeam(Team.CT);
            }
        }

        PlaySound();

        core.PlayerManager.SendCenter("Немезида => " + nemesis.Controller.PlayerName);
    }

    public void End()
    {
        roundManager.SetRound(new None());

        core.PlayerManager.SendCenter("Раунд окончен");
    }

    private void Initialize(IPlayer nemesis)
    {
        var zombieNemesis = zombieManager.GetZombie(nemesis.PlayerID);
        var zombieClass = zombieNemesis.GetZombieClass();
        var countPlayers = core.PlayerManager.GetAlive().Count();

        nemesis.SetHealth(zombieClass.Health + (config.NemesisBonusHealthPerPlayer * countPlayers));
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