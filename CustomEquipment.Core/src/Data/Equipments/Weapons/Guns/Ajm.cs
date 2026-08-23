using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Ajm : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Cz75A;

    public override AccessFlags AccessFlags => AccessFlags.Human;

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
            Head = 1.45f,
            Chest = 1.85f,
            Stomach = 1.75f,
            Arms = new DamageMultiplier.Arm(1.55f, 1.55f),
            Legs = new DamageMultiplier.Leg(1.65f, 1.65f),
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
        CycleTime = [0.07f, 0.08f],
    };

    public Price Price => new()
    {
        Item = 7500,
        Ammo = 375
    };

    public ItemRarity Rarity => ItemRarity.Uncommon;
}