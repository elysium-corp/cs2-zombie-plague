using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Round;

namespace ZombiePlague.Core.Data.Managers;

internal interface IHumanManager
{
    bool IsHuman(IPlayer player);

    bool IsSurvivor(IPlayer player);

    int GetHumanCount();

    void Respawn(IPlayer player);

    void SetSurvivor(IPlayer player, ISurvivorConfig roundSettings);

    void RegisterHooks();

    void UnregisterHooks();
}
