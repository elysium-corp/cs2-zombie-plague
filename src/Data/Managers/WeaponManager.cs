using CS2ZombiePlague.Data.Weapons;
using CS2ZombiePlague.Data.Weapons.Grenades;
using SwiftlyS2.Shared;

namespace CS2ZombiePlague.Data.Managers;

public class WeaponManager(ISwiftlyCore core, RoundManager roundManager, CommonUtils commonUtils)
{
    private readonly Dictionary<string, ICustomWeapon> _customWeapons = new();
    
    public void RegisterWeapons()
    {
        Register(new FrostNade(core, roundManager, commonUtils));
        Register(new BarrierNade(core, roundManager, commonUtils));
        Register(new JumpNade(core, commonUtils));
    }
    
    private void Register(ICustomWeapon weapon)
    {
        _customWeapons[weapon.OriginalName] = weapon;
        _customWeapons[weapon.OriginalName].Load();
    }
}