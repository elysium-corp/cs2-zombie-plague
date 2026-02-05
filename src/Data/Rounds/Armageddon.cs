using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Rounds;

public class Armageddon(
    ISwiftlyCore core,
    RoundManager roundManager,
    ZombieManager zombieManager,
    ArmageddonConfig config) : IRound
{
    private readonly KnifeManager _knifeManager = DependencyManager.GetService<KnifeManager>();

    public void Start()
    {
        var allPlayers = core.PlayerManager.GetAlive().Shuffle().ToList();
        var countPlayers = allPlayers.Count();

        for (int order = 0; order < countPlayers; order++)
        {
            (order < countPlayers / 2 ? (Action<IPlayer>) InitializeSurvivor : InitializeNemesis)(allPlayers[order]);
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

    private void InitializeSurvivor(IPlayer survivor)
    {
        var countPlayers = core.PlayerManager.GetAlive().Count();
        var playerPawn = survivor.RequiredPlayerPawn;

        survivor.SetHealth(playerPawn.Health + (config.SurvivorBonusHealthPerZombie * countPlayers));
        playerPawn.Render = new Color(0, 0, 255);

        var itemServices = playerPawn.ItemServices;
        if (itemServices == null) return;

        itemServices.RemoveItems();
        
        _knifeManager.GiveKnife(survivor);
        itemServices.GiveItem("weapon_negev");
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