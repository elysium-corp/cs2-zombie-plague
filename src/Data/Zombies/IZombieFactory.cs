using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Data.Zombies.ZClasses;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Zombies;

public interface IZombieFactory
{
    public Zombie Create(ISwiftlyCore core, ZombieManager zombieManager, IPlayer player, IZClass izClass,
        bool isNemesis = false);
}