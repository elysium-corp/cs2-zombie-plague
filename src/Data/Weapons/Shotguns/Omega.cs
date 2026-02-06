using CS2ZombiePlague.Data.Weapons.Contracts;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Shotguns;

public sealed class Omega : BaseWeapon
{
    public override string InheritorName => "weapon_xm1014";

    public override string DisplayName => "Omega Shotgun";
    
    public override gear_slot_t Slot => gear_slot_t.GEAR_SLOT_RIFLE;

    public override string Model => "weapons/nozb1/valogun/araxys_bundle/araxys_sawedoff/araxys_sawedoff_ag2.vmdl";

    public override string WeaponFireParticle => "particles/weapons/cs_weapon_fx/weapon_confetti_sparks_2.vpcf";

    public override WeaponFireParticleType WeaponFireParticleType => WeaponFireParticleType.Trail;
}