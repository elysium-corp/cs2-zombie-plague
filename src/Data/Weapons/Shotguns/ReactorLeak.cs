using CS2ZombiePlague.Data.Weapons.Contracts;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Shotguns;

public class ReactorLeak : BaseWeapon
{
    public override string InheritorName => "ump45";

    public override string DisplayName => "UMP45 ReactorLeak";
    
    public override gear_slot_t Slot => gear_slot_t.GEAR_SLOT_RIFLE;

    public override string Model => "weapons/luci/car_ump45/car_ump45_ag2.vmdl";

    public override float DamageMultiplier => 1.5f;
}