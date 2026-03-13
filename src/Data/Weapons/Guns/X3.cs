using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Enums;

namespace CS2ZombiePlague.Data.Weapons.Guns;

public sealed class X3 : BaseWeapon, IWeaponPurchasable
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