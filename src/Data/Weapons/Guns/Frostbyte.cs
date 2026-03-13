using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Enums;

namespace CS2ZombiePlague.Data.Weapons.Guns;

public sealed class Frostbyte : BaseWeapon, IWeaponPurchasable
{
    public override string InheritorName => "mp7";

    public override string DisplayName => "MP7 Frostbyte";

    public override string InternalName => "mp7_frostbyte";

    public override WeaponSlot Slot => WeaponSlot.Primary;

    public override string Model => "weapons/luci/eov_mp5/eov_mp5_ag2.vmdl";
    
    public override WeaponRarity WeaponRarity => WeaponRarity.Modified;

    public override float DamageMultiplier => 1.8f;

    public int Coast => 1;

    public WeaponType WeaponType => WeaponType.SubmachineGun;
}