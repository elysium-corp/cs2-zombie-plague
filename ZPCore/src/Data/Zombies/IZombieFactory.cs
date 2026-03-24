using ZPCore.Data.Managers;
using ZPCore.Data.Zombies.ZClasses;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Zombies;

internal interface IZombieFactory
{
    public Zombie Create(ISwiftlyCore core, ZombieManager zombieManager, IPlayer player, IZClass izClass,
        bool isNemesis = false);
}