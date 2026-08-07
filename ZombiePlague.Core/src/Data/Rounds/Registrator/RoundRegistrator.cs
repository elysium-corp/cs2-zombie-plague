using Microsoft.Extensions.Options;
using ZombiePlague.Core.Config.Round;

namespace ZombiePlague.Core.Data.Rounds.Registrator;

internal class RoundRegistrator(IOptions<RoundConfig> config) : IRoundRegistrator
{
    private readonly List<IRoundConfig> _rounds = [];

    public IEnumerable<IRoundConfig> GetAll()
    {
        return _rounds;
    }

    public IEnumerable<IRoundConfig> GetAllEnabled()
    {
        return _rounds.Where(IsRoundEnable).ToList();
    }
    
    public void Register()
    {
        _rounds.Clear();

        var rounds = config.Value.GetAll();
        
        _rounds.AddRange(rounds);
    }

    private bool IsRoundEnable(IRoundConfig roundConfig)
    {
        return roundConfig is { Enable: true, Weight: > 0 };
    }
}