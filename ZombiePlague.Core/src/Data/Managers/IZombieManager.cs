using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Zombies;

namespace ZombiePlague.Core.Data.Managers;

internal interface IZombieManager
{
    bool IsNemesis(IPlayer player);

    void SetNemesis(IPlayer player, INemesisConfig? roundConfig = null);

    IReadOnlyDictionary<int, IZombie> GetAllZombies();

    void Respawn(IPlayer player);

    IZombie? CreateZombie(IPlayer player, IPlayer? infector = null);

    IZombie? GetZombie(IPlayer player);

    void RegisterHooks();

    void UnregisterHooks();

    void RemoveAll();
}
