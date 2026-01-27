using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.ZClasses;

public interface IZombiePlayerFactory
{
    public ZombiePlayer Create(ISwiftlyCore core, ZombieManager zombieManager, IPlayer player, IZClass izClass,
        bool isNemesis = false);
}