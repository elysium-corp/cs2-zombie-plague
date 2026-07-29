using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Zombies.ZClasses;

namespace ZombiePlague.Core.Data.Zombies;

internal interface IZombieFactory
{
    public Zombie Create(IPlayer player, IZClass izClass, bool isNemesis = false);

    public Zombie Create<TClass>(IPlayer player, bool isNemesis = false) where TClass : IZClass;
}