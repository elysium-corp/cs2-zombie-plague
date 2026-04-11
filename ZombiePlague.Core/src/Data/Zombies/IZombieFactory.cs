using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Zombies.ZClasses;
using ZPCore.Data.Zombies;

namespace ZombiePlague.Core.Data.Zombies;

internal interface IZombieFactory
{
    public Zombie Create(ISwiftlyCore core, ZombieManager zombieManager, IPlayer player, IZClass izClass,
        bool isNemesis = false);
}