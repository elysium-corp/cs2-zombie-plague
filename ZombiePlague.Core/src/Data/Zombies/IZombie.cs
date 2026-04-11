using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Zombies.ZClasses;
using ZPCore.Data.Zombies.Controller;

namespace ZombiePlague.Core.Data.Zombies;

internal interface IZombie
{
    IPlayer Player { get; }
    
    IZClass ZClass { get; }
    
    ISoundController?SoundController { get; }
    
    public bool IsNemesis { get; }
}