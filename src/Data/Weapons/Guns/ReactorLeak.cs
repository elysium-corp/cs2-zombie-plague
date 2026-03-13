using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Enums;

namespace CS2ZombiePlague.Data.Weapons.Guns;

public sealed class ReactorLeak : BaseWeapon, IWeaponPurchasable
{
    public override string InheritorName => "ump45";

    public override string DisplayName => "UMP45 ReactorLeak";
    
    public override string InternalName => "ump45_reactor_leak";

    public override WeaponSlot Slot => WeaponSlot.Primary;

    public override string Model => "weapons/luci/car_ump45/car_ump45_ag2.vmdl";
    
    public override WeaponRarity WeaponRarity => WeaponRarity.Modified;

    public override float DamageMultiplier => 1.5f;

    public int Coast => 1;

    public WeaponType WeaponType => WeaponType.SubmachineGun;
}