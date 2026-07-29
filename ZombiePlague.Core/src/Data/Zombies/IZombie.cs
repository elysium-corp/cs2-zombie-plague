using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Zombies.Controller;
using ZombiePlague.Core.Data.Zombies.ZClasses;

namespace ZombiePlague.Core.Data.Zombies;

internal interface IZombie : IDisposable
{
    IPlayer Player { get; }
    
    IZClass ZClass { get; }
    
    ISoundController? SoundController { get; }
    
    bool IsNemesis { get; }

    void Initialize();
}
