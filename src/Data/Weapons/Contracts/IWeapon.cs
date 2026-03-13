using CS2ZombiePlague.Data.Weapons.Enums;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Contracts;

public interface IWeapon
{
    CCSWeaponBase AttachedWeapon { get; set; }
    
    string InheritorName { get; }
    
    string DisplayName { get; }
    
    string InternalName { get; }
    
    WeaponSlot Slot { get; }
    
    string Model { get; }
    
    WeaponRarity WeaponRarity { get; }
}