using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Data.Zombies.ZClasses;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Zombies;

public class ZombieFactory : IZombieFactory
{
    public Zombie Create(ISwiftlyCore core,ZombieManager zombieManager, IPlayer player, IZClass izClass, bool isNemesis = false)
    {
        return izClass switch
        {
            ZCleric zCleric => new Zombie(core, zombieManager, player, zCleric, isNemesis),
            ZHunter zHunter => new Zombie(core, zombieManager, player, zHunter, isNemesis),
            ZAssassin zAssassin => new Zombie(core, zombieManager, player, zAssassin, isNemesis),
            ZHeavy zHeavy => new Zombie(core, zombieManager, player, zHeavy, isNemesis),
            ZSmoker zSmoker => new Zombie(core, zombieManager, player, zSmoker, isNemesis),
            ZNemesis zombieNemesis => new Zombie(core, zombieManager, player, zombieNemesis, isNemesis),
            _ => new Zombie(core, zombieManager, player, DependencyManager.GetService<ZCleric>(), isNemesis)
        };
    }
}