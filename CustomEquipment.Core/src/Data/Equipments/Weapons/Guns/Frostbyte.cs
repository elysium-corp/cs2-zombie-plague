using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Frostbyte : WeaponItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Mp7;

    public override AccessFlags AccessFlags => AccessFlags.Human;
    
    public override string DisplayName => "MP7 Frostbyte";
    
    public override string InternalName => "custom_equipment:frostbyte";
    
    public override string SubclassName => "weapon_frostbyte";
    
    public override Slot Slot => Slot.Primary;
    
    public override WeaponType WeaponType => WeaponType.SubmachineGun;

    public override string Model => "weapons/luci/eov_mp5/eov_mp5_ag2.vmdl";

    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 1.8f,
            Chest = 1.15f,
        }
    };
    
    public override WeaponParticle Particle => new()
    {
        Trace = "particles/kolka/shoteffects/tracer3.vpcf"
    };
    
    public override Ammunition Ammunition => new()
    {
        Clip = 10,
        ReserveAmmo = 60
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