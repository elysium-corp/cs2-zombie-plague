using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;
using CustomEquipment.Data.Equipments.Particle;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Elite : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Ssg08;

    public override AccessFlags AccessFlags => AccessFlags.Human;
    
    public override string DisplayName => "SSG Elite";

    public override string InternalName => "custom_equipment:elite";

    public override string SubclassName => "weapon_elite_v2";

    public override Slot Slot => Slot.Primary;

    public override WeaponType WeaponType => WeaponType.Rifle;

    public override string Model => "weapons/luci/parab_ssg/parab_ssg_ag2.vmdl";

    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 2.55f,
            Chest = 3.55f,
            Stomach = 3.55f,
            Arms = new DamageMultiplier.Arm(2.25f, 2.25f),
            Legs = new DamageMultiplier.Leg(2.45f, 2.45f),
        },
        NumBullets = 1,
        Penetration = 1,
        Range = 10_000f,
        RangeModifier = 1.0f
    };
    
    public override WeaponParticle Particle => new()
    {
        Trace = "particles/kolka/shoteffects/tracer11.vpcf"
    };
    
    public override Ammunition Ammunition => new()
    {
        Clip = 3,
        ReserveAmmo = 5
    };

    public override WeaponTiming WeaponTiming => new()
    {
        CycleTime = [1.455f, 1.455f],
    };

    public Price Price => new()
    {
        Item = 15000,
        Ammo = 750
    };

    public ItemRarity Rarity => ItemRarity.Uncommon;
}