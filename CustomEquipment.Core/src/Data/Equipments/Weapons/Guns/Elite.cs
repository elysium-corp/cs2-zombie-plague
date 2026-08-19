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
            Head = 6.0f,
            Chest = 1.45f,
        },
        NumBullets = 2,
        Penetration = 5,
        Range = 10_000f,
        RangeModifier = 1.0f
    };
    
    public override WeaponParticle Particle => new()
    {
        Trace = "particles/kolka/shoteffects/tracer11.vpcf"
    };
    
    public override Ammunition Ammunition => new()
    {
        Clip = 20,
        ReserveAmmo = 5
    };

    public override WeaponTiming WeaponTiming => new()
    {
        CycleTime = [0.2f, 1.0f],
    };

    public Price Price => new()
    {
        Item = 1500,
        Ammo = 100
    };

    public ItemRarity Rarity => ItemRarity.Uncommon;
}