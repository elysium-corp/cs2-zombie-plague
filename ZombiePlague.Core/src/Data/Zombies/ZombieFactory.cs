using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Zombies.ZClasses;

namespace ZombiePlague.Core.Data.Zombies;

internal class ZombieFactory(ISwiftlyCore core, IZClassFactory zClassFactory, IZombieManager zombieManager) : IZombieFactory
{
    public Zombie Create(IPlayer player, IZClass izClass, bool isNemesis = false)
    {
        return izClass switch
        {
            ZCleric zCleric => new Zombie(core, zombieManager, player, zCleric, isNemesis),
            ZHunter zHunter => new Zombie(core, zombieManager, player, zHunter, isNemesis),
            ZAssassin zAssassin => new Zombie(core, zombieManager, player, zAssassin, isNemesis),
            ZHeavy zHeavy => new Zombie(core, zombieManager, player, zHeavy, isNemesis),
            ZSmoker zSmoker => new Zombie(core, zombieManager, player, zSmoker, isNemesis),
            ZNemesis zNemesis => new Zombie(core, zombieManager, player, zNemesis, isNemesis),
            _ => throw new NotSupportedException("ZombieFactory: parameter izClass hasn't supported!")
        };
    }

    public Zombie Create<TClass>(IPlayer player, bool isNemesis = false) where TClass : IZClass
    {
        return zClassFactory.Create<TClass>() switch
        {
            ZCleric zCleric => new Zombie(core, zombieManager, player, zCleric, isNemesis),
            ZHunter zHunter => new Zombie(core, zombieManager, player, zHunter, isNemesis),
            ZAssassin zAssassin => new Zombie(core, zombieManager, player, zAssassin, isNemesis),
            ZHeavy zHeavy => new Zombie(core, zombieManager, player, zHeavy, isNemesis),
            ZSmoker zSmoker => new Zombie(core, zombieManager, player, zSmoker, isNemesis),
            ZNemesis zNemesis => new Zombie(core, zombieManager, player, zNemesis, isNemesis),
            _ => throw new NotSupportedException("ZombieFactory: type TClass hasn't supported!")
        };
    }
}