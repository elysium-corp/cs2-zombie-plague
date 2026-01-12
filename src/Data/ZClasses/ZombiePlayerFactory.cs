using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.ZClasses;

public class ZombiePlayerFactory : IZombiePlayerFactory
{
    public ZombiePlayer Create(IPlayer player, ZombieManager zombieManager, IZClass izClass, bool isNemesis = false)
    {
        return izClass switch
        {
            ZCleric zCleric => new ZombiePlayer(player, zombieManager, zCleric, isNemesis),
            ZHunter zHunter => new ZombiePlayer(player, zombieManager, zHunter, isNemesis),
            ZAssassin zAssassin => new ZombiePlayer(player, zombieManager, zAssassin, isNemesis),
            ZHeavy zHeavy => new ZombiePlayer(player, zombieManager, zHeavy, isNemesis),
            ZShaman zShaman => new ZombiePlayer(player, zombieManager, zShaman, isNemesis),
            ZNemesis zombieNemesis => new ZombiePlayer(player, zombieManager, zombieNemesis, isNemesis),
            _ => new ZombiePlayer(player, zombieManager, DependencyManager.GetService<ZCleric>(), isNemesis)
        };
    }
}