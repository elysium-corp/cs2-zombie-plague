using CS2ZombiePlague.Data.Weapons.Controller;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Lifecycle;

public class PlayerLifecycle(IPlayer player) : IPlayerLifecycle
{
    public IPlayer Player => player;
    public IWeaponController? WeaponController { get; set; }
    
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
        WeaponController?.Dispose();
        WeaponController = null;
    }
}