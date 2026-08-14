using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Lava : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Ak47;

    public override string DisplayName => "AK47 Lava";

    public override string InternalName => "custom_equipment:lava";

    public override string SubclassName => "weapon_ak_117_lava";

    public override Slot Slot => Slot.Primary;

    public override WeaponType WeaponType => WeaponType.SubmachineGun;
    
    public override string Model => "weapons/luci/ak_117_lava/ak_117_lava.vmdl";
    
    public override WeaponParticle Particle => new()
    {
        Trace = "particles/kolka/shoteffects/tracer1.vpcf"
    };

    public override Ammunition Ammunition => new()
    {
        Clip = 25,
        ReserveAmmo = 10
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