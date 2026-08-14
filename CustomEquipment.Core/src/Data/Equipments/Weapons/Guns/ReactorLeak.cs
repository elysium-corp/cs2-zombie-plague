using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class ReactorLeak : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Ump45;
    
    public override string DisplayName => "UMP45 ReactorLeak";
    
    public override string InternalName => "custom_equipment:reactorleak";
    
    public override string SubclassName => "weapon_reactorleak";
    
    public override Slot Slot => Slot.Primary;
    
    public override WeaponType WeaponType => WeaponType.SubmachineGun;

    public override string Model => "weapons/luci/car_ump45/car_ump45_ag2.vmdl";
    
    public override WeaponParticle Particle => new()
    {
        Trace = "particles/kolka/shoteffects/tracer8.vpcf"
    };
    
    public override Ammunition Ammunition => new()
    {
        Clip = 20,
        ReserveAmmo = 60
    };
    
    public override WeaponTiming WeaponTiming => new()
    {
        CycleTime = [0.15f, 1.0f],
    };
    
    public Price Price => new()
    {
        Item = 1500,
        Ammo = 100
    };

    public ItemRarity Rarity => ItemRarity.Uncommon;
}