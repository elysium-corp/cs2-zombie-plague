using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Contracts;

public interface IWeapon
{
    CCSWeaponBase InheritorWeapon { get; set; }
    
    string DisplayName { get; }
    
    string InheritorName { get; }
    
    gear_slot_t Slot { get; }
    
    string Model { get; }
    
    float DamageMultiplier { get; }
}