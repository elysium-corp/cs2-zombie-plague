using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.ZClasses;

public class ZombiePlayerFactory : IZombiePlayerFactory
{
    public ZombiePlayer Create(ISwiftlyCore core,ZombieManager zombieManager, IPlayer player, IZClass izClass, bool isNemesis = false)
    {
        return izClass switch
        {
            ZCleric zCleric => new ZombiePlayer(core, zombieManager, player, zCleric, isNemesis),
            ZHunter zHunter => new ZombiePlayer(core, zombieManager, player, zHunter, isNemesis),
            ZAssassin zAssassin => new ZombiePlayer(core, zombieManager, player, zAssassin, isNemesis),
            ZHeavy zHeavy => new ZombiePlayer(core, zombieManager, player, zHeavy, isNemesis),
            ZShaman zShaman => new ZombiePlayer(core, zombieManager, player, zShaman, isNemesis),
            ZNemesis zombieNemesis => new ZombiePlayer(core, zombieManager, player, zombieNemesis, isNemesis),
            _ => new ZombiePlayer(core, zombieManager, player, DependencyManager.GetService<ZCleric>(), isNemesis)
        };
    }
}