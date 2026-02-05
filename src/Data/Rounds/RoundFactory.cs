using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared;

namespace CS2ZombiePlague.Data.Rounds;

public class RoundFactory(ISwiftlyCore core, ZombieManager zombieManager, CommonUtils commonUtils) : IRoundFactory
{
    public IRound Create(IRoundConfig? config, RoundManager roundManager)
    {
        return config switch
        {
            InfectionConfig roundConfig => new Infection(core, roundManager, zombieManager, commonUtils, roundConfig),
            NemesisConfig roundConfig => new Nemesis(core, roundManager, zombieManager, roundConfig),
            PlagueConfig roundConfig => new Plague(core, roundManager, zombieManager, commonUtils, roundConfig),
            SurvivorConfig roundConfig => new Survivor(core, roundManager, zombieManager, roundConfig),
            ArmageddonConfig roundConfig => new Armageddon(core, roundManager, zombieManager, roundConfig),
            _ => new None()
        };
    }
}