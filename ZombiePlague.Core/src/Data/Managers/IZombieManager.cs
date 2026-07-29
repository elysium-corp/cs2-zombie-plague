using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Zombies;

namespace ZombiePlague.Core.Data.Managers;

public interface IZombieManager
{
    bool IsNemesis(IPlayer player);

    public void SetNemesis(IPlayer player, INemesisConfig? roundConfig = null);

    public Dictionary<int, IZombie> GetAllZombies();

    public void Respawn(IPlayer player);

    public IZombie? CreateZombie(IPlayer player, IPlayer? infector = null);

    IZombie? GetZombie(IPlayer player);

    void RegisterHooks();
}