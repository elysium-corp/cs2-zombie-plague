using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers.Contracts;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal class RoundFactory(
    ISwiftlyCore core, 
    IOptions<RoundConfig> config, 
    IPlayerManager playerManager,
    IEventPublisher eventPublisher
) : IRoundFactory
{
    public RoundBase Create<TRound>() where TRound : RoundBase
    {
        return typeof(TRound) switch
        {
            var type when type == typeof(Infection) => new Infection(core, playerManager, eventPublisher, config.Value.Infection),
            var type when type == typeof(Plague) => new Plague(core, playerManager, eventPublisher, config.Value.Plague),
            var type when type == typeof(Nemesis) => new Nemesis(core, playerManager, eventPublisher, config.Value.Nemesis),
            var type when type == typeof(Survivor) => new Survivor(core, playerManager, eventPublisher, config.Value.Survivor),
            _ => throw new NotSupportedException($"RoundFactory: type '{typeof(TRound)}' is not supported.")
        };
    }

    public RoundBase Create(IRoundConfig roundConfig)
    {
        return roundConfig switch
        {
            InfectionConfig value => new Infection(core, playerManager, eventPublisher, value),
            PlagueConfig value => new Plague(core, playerManager, eventPublisher, value),
            NemesisConfig value => new Nemesis(core, playerManager, eventPublisher, value),
            SurvivorConfig value => new Survivor(core, playerManager, eventPublisher, value),
            _ => throw new NotSupportedException($"RoundFactory: config '{config.GetType().Name}' is not supported.")
        };
    }
}