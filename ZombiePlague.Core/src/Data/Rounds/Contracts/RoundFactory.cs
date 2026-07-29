using SwiftlyS2.Shared;
using ZombiePlague.Api.Data;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers;
using ZPCore.Config.Round;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal class RoundFactory(ISwiftlyCore core, IZombieManager zombieManager, HumanManager humanManager) : IRoundFactory
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