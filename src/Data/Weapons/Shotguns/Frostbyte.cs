using CS2ZombiePlague.Data.Weapons.Contracts;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Shotguns;

public class Frostbyte : BaseWeapon
{
    public override string InheritorName => "mp7";

    public override string DisplayName => "MP7 Frostbyte";
    
    public override gear_slot_t Slot => gear_slot_t.GEAR_SLOT_RIFLE;

    public override string Model => "weapons/luci/eov_mp5/eov_mp5_ag2.vmdl";

    public override float DamageMultiplier => 1.8f;
}