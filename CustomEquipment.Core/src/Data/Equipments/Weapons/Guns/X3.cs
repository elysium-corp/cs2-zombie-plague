using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class X3 : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.M4A1S;

    public override AccessFlags AccessFlags => AccessFlags.Human;
    
    public override string DisplayName => "M4A1-S X3";
    
    public override string InternalName => "custom_equipment:x3";
    
    public override string SubclassName => "weapon_x3";
    
    public override Slot Slot => Slot.Primary;
    
    public override WeaponType WeaponType => WeaponType.Rifle;

    public override string Model => "weapons/luci/x3_m4a1/x3_m4a1_ag2.vmdl";
    
    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 2.05f,
            Chest = 2.85f,
            Stomach = 2.6f,
            Arms = new DamageMultiplier.Arm(2.45f, 2.45f),
            Legs = new DamageMultiplier.Leg(2.85f, 2.85f),
        },
        NumBullets = 1,
        Penetration = 1,
        Range = 10_000f,
        RangeModifier = 1.0f
    };
    
    public override Ammunition Ammunition => new()
    {
        Clip = 25,
        ReserveAmmo = 7
    };
    
    public Price Price => new()
    {
        Item = 16500,
        Ammo = 550
    };

    public ItemRarity Rarity => ItemRarity.Uncommon;
}