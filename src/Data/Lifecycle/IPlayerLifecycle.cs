using CS2ZombiePlague.Data.Weapons.Controller;
using CS2ZombiePlague.Data.Zombies.Controller;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Lifecycle;

public interface IPlayerLifecycle : ILifecycle
{
    IPlayer Player { get; }

    IWeaponController? WeaponController { get; set; }
    
    ISoundController? SoundController { get; set; }

    void Bind();
}