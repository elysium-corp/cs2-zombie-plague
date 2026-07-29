using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Zombies.Controller;
using ZombiePlague.Core.Data.Zombies.ZClasses;

namespace ZombiePlague.Core.Data.Zombies;

public interface IZombie
{
    IPlayer Player { get; }
    
    IZClass ZClass { get; }
    
    ISoundController? SoundController { get; }
    
    public bool IsNemesis { get; }

    public void Initialize();

    public void UnHookAbilities();
}