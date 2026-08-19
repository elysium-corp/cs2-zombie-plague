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
    
    public override WeaponParticle Particle => new()
    {
        Trace = "particles/kolka/shoteffects/tracer1.vpcf"
    };

    public override Ammunition Ammunition => new()
    {
        Clip = 5,
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