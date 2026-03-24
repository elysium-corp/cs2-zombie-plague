using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Enums;

namespace ZPCore.Data.Weapons.Guns;

internal sealed class X3 : BaseWeapon, IWeaponPurchasable
{
    public override string InheritorName => "m4a1_silencer";

    public override string DisplayName => "M4A1-S X3";
    
    public override string InternalName => "m4a1_silencer_x3";

    public override WeaponSlot Slot => WeaponSlot.Primary;

    public override string Model => "weapons/luci/x3_m4a1/x3_m4a1_ag2.vmdl";
    
    public override WeaponRarity WeaponRarity => WeaponRarity.Modified;

    public override float DamageMultiplier => 3.0f;

    public int Coast => 1;

    public WeaponType WeaponType => WeaponType.Rifle;
}