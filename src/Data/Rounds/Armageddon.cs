using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Rounds;

public class Armageddon(
    ISwiftlyCore core,
    RoundManager roundManager,
    ZombieManager zombieManager,
    HumanManager humanManager,
    ArmageddonConfig config) : IRound
{
    public void Start()
    {
        var allPlayers = core.PlayerManager.GetAlive().Shuffle().ToList();
        var countPlayers = allPlayers.Count();

        for (int order = 0; order < countPlayers; order++)
        {
            if (order < countPlayers / 2)
            {
                humanManager.SetSurvivor(allPlayers[order], config);
            }
            else
            {
                InitializeNemesis(allPlayers[order]);
            }
        }

        core.PlayerManager.SendCenter("Армагеддон");
    }

    public void End()
    {
        roundManager.SetRound(new None());

        core.PlayerManager.SendCenter("Раунд окончен");
    }

    public int GetChance()
    {
        return config.Chance;
    }

    private void InitializeNemesis(IPlayer nemesis)
    {
        zombieManager.CreateNemesis(nemesis);

        var zombieNemesis = zombieManager.GetZombie(nemesis.PlayerID);
        var zombieClass = zombieNemesis.GetZombieClass();
        var countPlayers = core.PlayerManager.GetAlive().Count() / 2;

        core.Scheduler.NextTick(() =>
        {
            nemesis.SetHealth(zombieClass.Health + (config.NemesisBonusHealthPerPlayer * countPlayers));
        });
    }
}