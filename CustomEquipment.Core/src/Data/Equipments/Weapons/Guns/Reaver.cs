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

    public override string DisplayName => "Deagle Reaver";

    public override string InternalName => "custom_equipment:reaver";

    public override string SubclassName => "weapon_reaver_deagle";

    public override Slot Slot => Slot.Secondary;

    public override WeaponType WeaponType => WeaponType.Pistol;

    public override string Model => "weapons/luci/reaver_deagle/reaver_deagle.vmdl";

    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 15.0f,
            Chest = 10.45f,
        },
        NumBullets = 1,
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
        Clip = 1,
        ReserveAmmo = 5
    };

    public override WeaponTiming WeaponTiming => new()
    {
        CycleTime = [1.0f, 1.0f],
    };
    
    public int Price => 4_500;

    public ItemRarity Rarity => ItemRarity.Uncommon;
}