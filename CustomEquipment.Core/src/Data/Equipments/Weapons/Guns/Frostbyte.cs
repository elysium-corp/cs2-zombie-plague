using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Data.Equipments.Enums;
using CustomEquipment.Data.Equipments.Models;
using CustomEquipment.Data.Equipments.Particle;

namespace CustomEquipment.Data.Equipments.Weapons.Guns;

internal sealed class Frostbyte : BaseWeapon
{
    public override string InheritorName => WeaponName.Mp7;

    public override string DisplayName => "MP7 Frostbyte";
    
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
}