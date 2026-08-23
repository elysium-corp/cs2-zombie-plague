using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Frostbyte : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Mp7;

    public override AccessFlags AccessFlags => AccessFlags.Human;
    
    public override string DisplayName => "MP7 Frostbyte";
    
    public override string InternalName => "custom_equipment:frostbyte";
    
    public override string SubclassName => "weapon_frostbyte";
    
    public override Slot Slot => Slot.Primary;
    
    public override WeaponType WeaponType => WeaponType.SubmachineGun;

    public override string Model => "weapons/luci/eov_mp5/eov_mp5_ag2.vmdl";
    
    public override WeaponParticle Particle => new()
    {
        Trace = "particles/kolka/shoteffects/tracer1.vpcf"
    };
    
    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 2.95f,
            Chest = 3.10f,
            Stomach = 3.10f,
            Arms = new DamageMultiplier.Arm(3.45f, 3.45f),
            Legs = new DamageMultiplier.Leg(3.85f, 3.85f),
        },
        NumBullets = 1,
        Penetration = 1,
        Range = 10_000f,
        RangeModifier = 1.0f
    };
    
    public override Ammunition Ammunition => new()
    {
        Clip = 10,
        ReserveAmmo = 5
    };
    
    public override WeaponTiming WeaponTiming => new()
    {
        CycleTime = [0.2f, 0.6f],
    };
    
    public Price Price => new()
    {
        Item = 13500,
        Ammo = 450
    };

    public ItemRarity Rarity => ItemRarity.Uncommon;
}