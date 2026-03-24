using SwiftlyS2.Shared.Players;
using ZPCore.Data.Zombies.ZClasses;

namespace ZPCore.Data.Zombies;

internal interface IZombie
{
    IPlayer Player { get; }
    
    IZClass ZClass { get; }
    
    public bool IsNemesis { get; }
}