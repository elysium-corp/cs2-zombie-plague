using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Blackline : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Mp9;

    public override AccessFlags AccessFlags => AccessFlags.Human;

    public override string DisplayName => "MP9 Blackline";

    public override string InternalName => "custom_equipment:blackline";

    public override string SubclassName => "weapon_blackline";

    public override Slot Slot => Slot.Primary;

    public override WeaponType WeaponType => WeaponType.SubmachineGun;

    public override string Model => "weapons/luci/psd_mp9/psd_mp9_ag2.vmdl";
    
    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 1.95f,
            Chest = 2.75f,
            Stomach = 2.35f,
            Arms = new DamageMultiplier.Arm(2.15f, 2.15f),
            Legs = new DamageMultiplier.Leg(2.35f, 2.35f),
        },
        NumBullets = 1,
        Penetration = 1,
        Range = 10_000f,
        RangeModifier = 1.0f
    };

    public override Ammunition Ammunition => new()
    {
        Clip = 20,
        ReserveAmmo = 8
    };

    public override WeaponTiming WeaponTiming => new()
    {
        CycleTime = [0.1f, 0.15f],
    };

    public Price Price => new()
    {
        Item = 10500,
        Ammo = 350
    };

    public ItemRarity Rarity => ItemRarity.Uncommon;
}