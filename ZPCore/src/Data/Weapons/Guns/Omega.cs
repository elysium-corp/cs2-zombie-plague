using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Enums;

namespace ZPCore.Data.Weapons.Guns;

internal sealed class Omega : BaseWeapon, IWeaponPurchasable
{
    public override string InheritorName => "xm1014";

    public override string DisplayName => "Omega Shotgun";
    
    public override string InternalName => "omega";

    public override WeaponSlot Slot => WeaponSlot.Primary;

    public override string Model => "weapons/nozb1/valogun/araxys_bundle/araxys_sawedoff/araxys_sawedoff_ag2.vmdl";
    
    public override WeaponRarity WeaponRarity => WeaponRarity.Modified;

    public int Coast => 1;

    public WeaponType WeaponType => WeaponType.Shotgun;
}