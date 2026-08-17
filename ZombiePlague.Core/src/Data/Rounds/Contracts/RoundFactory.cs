using Common.Hooks.Abstractions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers.Contracts;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal class RoundFactory(
    ISwiftlyCore core, 
    IOptions<RoundConfig> config, 
    IPlayerManager playerManager,
    IHookPublisher hooks
) : IRoundFactory
{
    public RoundBase Create<TRound>() where TRound : RoundBase
    {
        return typeof(TRound) switch
        {
            var type when type == typeof(Infection) => new Infection(core, playerManager, hooks, config.Value.Infection),
            var type when type == typeof(Plague) => new Plague(core, playerManager, hooks, config.Value.Plague),
            var type when type == typeof(Nemesis) => new Nemesis(core, playerManager, hooks, config.Value.Nemesis),
            var type when type == typeof(Survivor) => new Survivor(core, playerManager, hooks, config.Value.Survivor),
            _ => throw new NotSupportedException($"RoundFactory: type '{typeof(TRound)}' is not supported.")
        };
    }

    public RoundBase Create(IRoundConfig roundConfig)
    {
        return roundConfig switch
        {
            InfectionConfig value => new Infection(core, playerManager, hooks, value),
            PlagueConfig value => new Plague(core, playerManager, hooks, value),
            NemesisConfig value => new Nemesis(core, playerManager, hooks, value),
            SurvivorConfig value => new Survivor(core, playerManager, hooks, value),
            _ => throw new NotSupportedException($"RoundFactory: config '{config.GetType().Name}' is not supported.")
        };
    }
}