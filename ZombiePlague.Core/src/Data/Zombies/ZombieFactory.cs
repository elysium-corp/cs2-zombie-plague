using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Zombies.ZClasses;

namespace ZombiePlague.Core.Data.Zombies;

internal sealed class ZombieFactory(ISwiftlyCore core, IZClassFactory zClassFactory) : IZombieFactory
{
    public Zombie Create(IPlayer player, IZClass izClass, bool isNemesis = false)
    {
        return new Zombie(core, player, izClass, isNemesis);
    }

    public Zombie Create<TClass>(IPlayer player, bool isNemesis = false) where TClass : IZClass
    {
        return new Zombie(core, player, zClassFactory.Create<TClass>(), isNemesis);
    }
}
