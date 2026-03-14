using CS2ZombiePlague.Data.Zombies.ZClasses;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Zombies;

public interface IZombie
{
    IPlayer Player { get; }
    IZClass ZClass { get; }
    public bool IsNemesis { get; }
}