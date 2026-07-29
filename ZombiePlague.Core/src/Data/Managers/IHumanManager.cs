using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Round;

namespace ZombiePlague.Core.Data.Managers;

public interface IHumanManager
{
    public bool IsSurvivor(IPlayer player);

    public void SetSurvivor(IPlayer player, ISurvivorConfig roundSettings);

    public void RegisterHooks();
}