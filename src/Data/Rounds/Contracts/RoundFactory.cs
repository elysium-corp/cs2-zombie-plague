using CS2ZombiePlague.Config;
using CS2ZombiePlague.Config.Round;
using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared;

namespace CS2ZombiePlague.Data.Rounds.Contracts;

public class RoundFactory(ISwiftlyCore core, ZombieManager zombieManager, HumanManager humanManager) : IRoundFactory
{
    public IRound Create(IRoundConfig? config, RoundManager roundManager)
    {
        return config switch
        {
            InfectionConfig roundConfig => new Infection(core, roundManager, zombieManager, roundConfig),
            NemesisConfig roundConfig => new Nemesis(core, roundManager, zombieManager, roundConfig),
            PlagueConfig roundConfig => new Plague(core, roundManager, zombieManager, roundConfig),
            SurvivorConfig roundConfig => new Survivor(core, roundManager, zombieManager, humanManager, roundConfig),
            ArmageddonConfig roundConfig => new Armageddon(core, roundManager, zombieManager, humanManager, roundConfig),
            _ => new None()
        };
    }
}