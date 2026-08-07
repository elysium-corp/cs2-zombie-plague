using ZombiePlague.Core.Config.Round;

namespace ZombiePlague.Core.Data.Rounds.Registrator;

internal interface IRoundRegistrator
{
    public IEnumerable<IRoundConfig> GetAll();

    public IEnumerable<IRoundConfig> GetAllEnabled();

    public void Register();
}