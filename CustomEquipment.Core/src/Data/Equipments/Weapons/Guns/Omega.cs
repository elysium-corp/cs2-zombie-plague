using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Omega : WeaponItemBase
{
    public override string InheritorName => WeaponName.Xm1014;
    
    public override string DisplayName => "Omega Shotgun";
    
    public override string InternalName => "custom_equipment:weapon_omega";
    
    public override string SubclassName => "weapon_omega";

    public override Slot Slot => Slot.Primary;
    
    public override WeaponType WeaponType => WeaponType.Shotgun;

    public override string Model => "weapons/nozb1/valogun/araxys_bundle/araxys_sawedoff/araxys_sawedoff_ag2.vmdl";
    
    public override WeaponParticle Particle => new()
    {
        Trace = "particles/kolka/shoteffects/tracer7.vpcf"
    };
    
    public override WeaponDamage WeaponDamage => new()
    {
        DamageMultiplier = new DamageMultiplier
        {
            Head = 6.0f,
            Chest = 1.45f,
        },
        NumBullets = 2,
        Penetration = 5,
        Range = 10_000f,
        RangeModifier = 1.0f
    };
    
    public override Ammunition Ammunition => new()
    {
        Clip = 3,
        ReserveAmmo = 8
    };
    
    public override WeaponTiming WeaponTiming => new()
    {
        CycleTime = [0.8f, 1.0f],
    };
}