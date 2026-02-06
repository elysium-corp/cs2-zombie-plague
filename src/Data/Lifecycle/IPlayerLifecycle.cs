using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Data.Weapons.Controller;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Lifecycle;

public interface IPlayerLifecycle : ILifecycle
{
    IPlayer Player { get; }

    IWeaponController? WeaponController { get; set; }

    void Bind();
}