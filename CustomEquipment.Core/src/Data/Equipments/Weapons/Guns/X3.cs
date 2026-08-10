using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class X3 : WeaponBase
{
    public override string InheritorName => WeaponName.M4A1S;

    public override string DisplayName => "M4A1-S X3";
    
    public override string SubclassName => "weapon_x3";
    
    public override Slot Slot => Slot.Primary;
    
    public override WeaponType WeaponType => WeaponType.Rifle;

    public override string Model => "weapons/luci/x3_m4a1/x3_m4a1_ag2.vmdl";
    
    public override WeaponParticle Particle => new()
    {
        Trace = "particles/kolka/shoteffects/tracer10.vpcf"
    };
    
    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 3.0f,
            Chest = 2.45f,
            Stomach = 2.5f,
        }
    };
    
    public override Ammunition Ammunition => new()
    {
        Clip = 150,
        ReserveAmmo = 15
    };
}