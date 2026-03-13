using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Enums;

namespace CS2ZombiePlague.Data.Weapons.Guns;

public sealed class Elite : BaseWeapon, IWeaponPurchasable
{
    public override string InheritorName => "ssg08";

    public override string DisplayName => "SSG Elite";

    public override string InternalName => "ssg_elite";

    public override WeaponSlot Slot => WeaponSlot.Primary;

    public override string Model => "weapons/luci/parab_ssg/parab_ssg_ag2.vmdl";
    
    public override WeaponRarity WeaponRarity => WeaponRarity.Modified;

    public override float DamageMultiplier => 1.4f;

    public int Coast => 1;

    public WeaponType WeaponType => WeaponType.SniperRifle;
}