using ZPCore.Data.Weapons.Controller;
using ZPCore.Data.Zombies.Controller;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Lifecycle;

internal interface IPlayerLifecycle : ILifecycle
{
    IPlayer Player { get; }

    IWeaponController? WeaponController { get; set; }
    
    ISoundController? SoundController { get; set; }

    void Bind();
}