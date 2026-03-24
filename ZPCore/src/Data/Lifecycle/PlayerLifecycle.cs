using ZPCore.Data.Weapons.Controller;
using ZPCore.Data.Zombies.Controller;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Lifecycle;

internal class PlayerLifecycle(IPlayer player) : IPlayerLifecycle
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