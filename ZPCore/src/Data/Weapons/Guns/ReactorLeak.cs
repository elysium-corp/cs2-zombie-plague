using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Enums;

namespace ZPCore.Data.Weapons.Guns;

internal sealed class ReactorLeak : BaseWeapon, IWeaponPurchasable
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