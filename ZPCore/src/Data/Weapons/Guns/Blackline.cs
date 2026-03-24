using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Enums;

namespace ZPCore.Data.Weapons.Guns;

internal sealed class Blackline : BaseWeapon, IWeaponPurchasable
{
    public override string InheritorName => "mp9";

    public override string DisplayName => "MP9 Blackline";

    public override string InternalName => "mp9_blackline";

    public override WeaponSlot Slot => WeaponSlot.Primary;

    public override string Model => "weapons/luci/psd_mp9/psd_mp9_ag2.vmdl";

    public override WeaponRarity WeaponRarity => WeaponRarity.Modified;

    public override float DamageMultiplier => 1.3f;

    public int Coast => 1;

    public WeaponType WeaponType => WeaponType.SubmachineGun;
}