using Common.Effects;
using Common.Effects.Effects;
using Common.Effects.Effects.Settings;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Infection(
    ISwiftlyCore core,
    RoundManager roundManager,
    ZombieManager zombieManager,
    InfectionConfig config) : InfectiousRound(core, roundManager, zombieManager)
{
    public override int Chance => config.Chance;
    public override string Name => "Инфекция";

    protected override bool ZombieRevived => config.ZombieRevived;
    protected override float ZombieSpawnTime => config.ZombieSpawnTime;

    protected override void OnInfectiousStart()
    {
        var players = Core.PlayerManager.GetAlive().ToList();
        IPlayer firstZombie;

        if (ZombieManager.GetAllZombies().Any())
        {
            firstZombie = ZombieManager.GetAllZombies().First().Value.Player;
        }
        else
        {
            firstZombie = players[Random.Shared.Next(0, players.Count)];
            ZombieManager.CreateZombie(firstZombie);
        }

        if (config.FirstZombieIsInvisible)
        {
            var effectService = EffectService.Provide(Core);
            effectService.ApplyEffect<Vanish>(null, firstZombie, new VanishSettings(config.InvisibleDuration));
        }
        
        SoundExt.PlayAt(firstZombie, config.MusicSoundName, 2);

        firstZombie.SetHealth((int)(firstZombie.PlayerPawn!.Health * config.FirstZombieHealthRatio));

        Core.PlayerManager.SendCenter("Первый заражённый => " + firstZombie.Name);
    }
}