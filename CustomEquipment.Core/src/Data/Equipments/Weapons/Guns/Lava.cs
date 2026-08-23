using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Lava : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Ak47;

    public override AccessFlags AccessFlags => AccessFlags.Human;
    
    public override string DisplayName => "AK47 Lava";

    public override string InternalName => "custom_equipment:lava";

    public override string SubclassName => "weapon_ak_117_lava";

    public override Slot Slot => Slot.Primary;

    public override WeaponType WeaponType => WeaponType.SubmachineGun;
    
    public override string Model => "weapons/luci/ak_117_lava/ak_117_lava.vmdl";
    
    public override WeaponParticle Particle => new()
    {
        Trace = "particles/kolka/shoteffects/tracer7.vpcf"
    };
    
    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 2.45f,
            Chest = 2.85f,
            Stomach = 2.85f,
            Arms = new DamageMultiplier.Arm(2.65f, 2.65f),
            Legs = new DamageMultiplier.Leg(3.15f, 3.15f),
        },
        NumBullets = 1,
        Penetration = 1,
        Range = 10_000f,
        RangeModifier = 1.0f
    };

    public override Ammunition Ammunition => new()
    {
        Clip = 25,
        ReserveAmmo = 10
    };
    
    public override WeaponTiming WeaponTiming => new()
    {
        CycleTime = [0.12f, 0.13f],
    };
    
    public Price Price => new()
    {
        Item = 19500,
        Ammo = 650
    };

    public ItemRarity Rarity => ItemRarity.Uncommon;
}