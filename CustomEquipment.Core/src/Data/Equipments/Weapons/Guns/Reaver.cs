using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;
using CustomEquipment.Data.Equipments.Particle;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Reaver : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Deagle;

    public override AccessFlags AccessFlags => AccessFlags.Human;
    
    public override string DisplayName => "Deagle Reaver";

    public override string InternalName => "custom_equipment:reaver";

    public override string SubclassName => "weapon_reaver_deagle";

    public override Slot Slot => Slot.Secondary;

    public override WeaponType WeaponType => WeaponType.Pistol;

    public override string Model => "weapons/luci/reaver_deagle/reaver_deagle.vmdl";
    
    public override WeaponParticle Particle => new()
    {
        Trace = "particles/kolka/shoteffects/tracer11.vpcf"
    };
    
    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 11.55f,
            Chest = 8.45f,
            Stomach = 8.45f,
            Arms = new DamageMultiplier.Arm(9.45f, 9.45f),
            Legs = new DamageMultiplier.Leg(10.45f, 10.45f),
        },
        NumBullets = 1,
        Penetration = 1,
        Range = 10_000f,
        RangeModifier = 1.0f
    };
    
    public override Ammunition Ammunition => new()
    {
        Clip = 1,
        ReserveAmmo = 5
    };

    public override WeaponTiming WeaponTiming => new()
    {
        CycleTime = [1.5f, 1.6f],
    };
    
    public Price Price => new()
    {
        Item = 9500,
        Ammo = 450
    };

    public ItemRarity Rarity => ItemRarity.Uncommon;
}