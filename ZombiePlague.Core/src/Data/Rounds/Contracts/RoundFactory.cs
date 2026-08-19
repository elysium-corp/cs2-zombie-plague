using System.Diagnostics.CodeAnalysis;
using Common.Hooks.Abstractions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using ZombiePlague.Api.Data.Rounds;
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
            var type when type == typeof(Infection) => new Infection(core, playerManager, config.Value.Infection),
            var type when type == typeof(Plague) => new Plague(core, playerManager, config.Value.Plague),
            var type when type == typeof(Nemesis) => new Nemesis(core, playerManager, config.Value.Nemesis),
            var type when type == typeof(Survivor) => new Survivor(core, playerManager, config.Value.Survivor),
            
            _ => throw new NotSupportedException($"RoundFactory: type '{typeof(TRound)}' is not supported!")
        };
    }

    public RoundBase Create(IRoundConfig roundConfig)
    {
        return roundConfig switch
        {
            InfectionConfig value => new Infection(core, playerManager, value),
            PlagueConfig value => new Plague(core, playerManager, value),
            NemesisConfig value => new Nemesis(core, playerManager, value),
            SurvivorConfig value => new Survivor(core, playerManager, value),
            
            _ => throw new NotSupportedException($"RoundFactory: config '{roundConfig.GetType().Name}' is not supported!")
        };
    }

    public bool TryCreate(string roundId, [NotNullWhen(true)] out RoundBase? round)
    {
        round = roundId switch
        {
            RoundIds.Infection => Create<Infection>(),
            RoundIds.Plague => Create<Plague>(),
            RoundIds.Nemesis => Create<Nemesis>(),
            RoundIds.Survivor => Create<Survivor>(),

            _ => null
        };

        return round is not null;
    }
}