using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Data.Coordinators.Contracts;

public interface IPlayerPreferencesCoordinator
{
    public void Initialize(IPlayer player);

    public void SaveAndRemove(IPlayer player);

    public void SaveAllAndWait();
}