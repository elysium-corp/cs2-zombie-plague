using CS2ZombiePlague.Data.Weapons.Contracts;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Shotguns;

public class X3 : BaseWeapon
{
    public override string InheritorName => "m4a1_silencer";

    public override string DisplayName => "M4A1-S X3";
    
    public override gear_slot_t Slot => gear_slot_t.GEAR_SLOT_RIFLE;

    public override string Model => "weapons/luci/x3_m4a1/x3_m4a1_ag2.vmdl";

    public override float DamageMultiplier => 3.0f;
}