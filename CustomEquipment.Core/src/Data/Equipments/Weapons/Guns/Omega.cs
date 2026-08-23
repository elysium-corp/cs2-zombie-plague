using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Omega : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Xm1014;
    
    public override AccessFlags AccessFlags => AccessFlags.Human;
    
    public override string DisplayName => "Omega Shotgun";
    
    public override string InternalName => "custom_equipment:omega";
    
    public override string SubclassName => "weapon_omega";

    public override Slot Slot => Slot.Primary;
    
    public override WeaponType WeaponType => WeaponType.Shotgun;

    public override string Model => "weapons/nozb1/valogun/araxys_bundle/araxys_sawedoff/araxys_sawedoff_ag2.vmdl";
    
    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 1.65f,
            Chest = 1.85f,
            Stomach = 1.85f,
            Arms = new DamageMultiplier.Arm(2.05f, 2.05f),
            Legs = new DamageMultiplier.Leg(2.15f, 2.15f),
        },
        NumBullets = 1,
        Penetration = 1,
        Range = 10_000f,
        RangeModifier = 1.0f
    };
    
    public override Ammunition Ammunition => new()
    {
        Clip = 2,
        ReserveAmmo = 6
    };
    
    public override WeaponTiming WeaponTiming => new()
    {
        CycleTime = [0.8f, 1.0f],
    };
    
    public Price Price => new()
    {
        Item = 14000,
        Ammo = 700
    };

    public ItemRarity Rarity => ItemRarity.Uncommon;
}