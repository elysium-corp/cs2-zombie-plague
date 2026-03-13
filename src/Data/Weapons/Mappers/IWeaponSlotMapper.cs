using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Enums;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Mappers;

public interface IWeaponSlotMapper
{
    WeaponSlot MapTo(gear_slot_t slot);

    gear_slot_t MapTo(WeaponSlot slot);
}