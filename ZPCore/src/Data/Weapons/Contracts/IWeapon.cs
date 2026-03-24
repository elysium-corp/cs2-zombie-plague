using ZPCore.Data.Weapons.Enums;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZPCore.Data.Weapons.Contracts;

internal interface IWeapon
{
    CCSWeaponBase AttachedWeapon { get; set; }
    
    string InheritorName { get; }
    
    string DisplayName { get; }
    
    string InternalName { get; }
    
    WeaponSlot Slot { get; }
    
    string Model { get; }
    
    WeaponRarity WeaponRarity { get; }
}