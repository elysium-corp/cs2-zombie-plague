using SwiftlyS2.Shared;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Rounds.Contracts;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Plague(
    ISwiftlyCore core,
    RoundManager roundManager,
    ZombieManager zombieManager,
    PlagueConfig config) : InfectiousRound(core, roundManager, zombieManager)
{
    public override int Chance => config.Chance;
    public override string Name => "Чума";
    protected override bool ZombieRevived => config.ZombieRevived;
    protected override float ZombieSpawnTime => config.ZombieSpawnTime;

    protected override void OnInfectiousStart()
    {
        var players = Core.PlayerManager.GetAlive().ToList();
        var countZombies = Math.Ceiling(players.Count * config.ZombieSpawnRatio);
        var newPlayers = players.Shuffle();

        foreach (var player in newPlayers)
        {
            if (player.IsValid)
            {
                ZombieManager.CreateZombie(player);
                countZombies--;
            }

            if (countZombies == 0)
            {
                break;
            }
        }

        Core.PlayerManager.SendCenter("Массовое заражение!");
    }
}