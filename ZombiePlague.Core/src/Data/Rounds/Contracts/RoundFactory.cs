using SwiftlyS2.Shared;
using ZombiePlague.Api.Data;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers;
using ZPCore.Config.Round;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal sealed class RoundFactory(
    ISwiftlyCore core,
    IZombieManager zombieManager,
    IHumanManager humanManager) : IRoundFactory
{
    public IRound Create(IRoundConfig? config)
    {
        return config switch
        {
            InfectionConfig roundConfig => new Infection(core, zombieManager, humanManager, roundConfig),
            NemesisConfig roundConfig => new Nemesis(core, zombieManager, roundConfig),
            PlagueConfig roundConfig => new Plague(core, zombieManager, humanManager, roundConfig),
            SurvivorConfig roundConfig => new Survivor(core, zombieManager, humanManager, roundConfig),
            ArmageddonConfig roundConfig => new Armageddon(core, zombieManager, humanManager, roundConfig),
            _ => new None()
        };
    }
}
