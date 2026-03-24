using ZPCore.Config.Round;
using ZPCore.Data.Managers;
using SwiftlyS2.Shared;
using ZPApi.Data;

namespace ZPCore.Data.Rounds.Contracts;

internal class RoundFactory(ISwiftlyCore core, ZombieManager zombieManager, HumanManager humanManager) : IRoundFactory
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