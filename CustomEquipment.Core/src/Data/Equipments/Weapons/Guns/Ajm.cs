using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Ajm : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Cz75A;

    public override string DisplayName => "CZ75 Ajm";

    public override string InternalName => "custom_equipment:ajm";

    public override string SubclassName => "weapon_ajm9_cz75";

    public override Slot Slot => Slot.Secondary;

    public override WeaponType WeaponType => WeaponType.Pistol;

    public override string Model => "weapons/luci/ajm9_cz75/ajm9_cz75.vmdl";

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