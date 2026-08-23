using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class ReactorLeak : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Ump45;
    
    public override AccessFlags AccessFlags => AccessFlags.Human;
    
    public override string DisplayName => "UMP45 ReactorLeak";
    
    public override string InternalName => "custom_equipment:reactorleak";
    
    public override string SubclassName => "weapon_reactorleak";
    
    public override Slot Slot => Slot.Primary;
    
    public override WeaponType WeaponType => WeaponType.SubmachineGun;

    public override string Model => "weapons/luci/car_ump45/car_ump45_ag2.vmdl";
    
    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 1.65f,
            Chest = 2.50f,
            Stomach = 2.50f,
            Arms = new DamageMultiplier.Arm(2.45f, 2.45f),
            Legs = new DamageMultiplier.Leg(2.55f, 2.55f),
        },
        NumBullets = 1,
        Penetration = 1,
        Range = 10_000f,
        RangeModifier = 1.0f
    };
    
    public override Ammunition Ammunition => new()
    {
        Clip = 20,
        ReserveAmmo = 5
    };
    
    public override WeaponTiming WeaponTiming => new()
    {
        CycleTime = [0.13f, 0.15f],
    };
    
    public Price Price => new()
    {
        Item = 9500,
        Ammo = 315
    };

    public ItemRarity Rarity => ItemRarity.Uncommon;
}