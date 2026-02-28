using CS2ZombiePlague.Data.Weapons.Controller;
using CS2ZombiePlague.Data.Zombies.Controller;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Lifecycle;

public class PlayerLifecycle(IPlayer player) : IPlayerLifecycle
{
    public IPlayer Player => player;
    public IWeaponController? WeaponController { get; set; }
    public ISoundController? SoundController { get; set; }

    public void Bind()
    {
        if (WeaponController != null)
        {
            return;
        }
        
        WeaponController = new WeaponController(player);
    }

    public void Dispose()
    {
        SoundController?.Dispose();
        SoundController = null;
        WeaponController?.Dispose();
        WeaponController = null;
    }
}