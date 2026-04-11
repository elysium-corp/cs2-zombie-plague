using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Data.Equipments.Enums;
using CustomEquipment.Data.Equipments.Models;
using CustomEquipment.Data.Equipments.Particle;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Blackline : BaseWeapon
{
    public override string InheritorName => WeaponName.Mp9;

    public override string DisplayName => "MP9 Blackline";

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
}