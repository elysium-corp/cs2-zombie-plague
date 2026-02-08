using CS2ZombiePlague.Data.Weapons.Contracts;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Shotguns;

public class Blackline : BaseWeapon
{
    public override string InheritorName => "mp9";

    public override string DisplayName => "MP9 Blackline";
    
    public override gear_slot_t Slot => gear_slot_t.GEAR_SLOT_RIFLE;

    public override string Model => "weapons/luci/psd_mp9/psd_mp9_ag2.vmdl";

    public override float DamageMultiplier => 1.3f;
}