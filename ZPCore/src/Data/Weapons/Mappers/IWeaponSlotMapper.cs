using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Enums;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZPCore.Data.Weapons.Mappers;

internal interface IWeaponSlotMapper
{
    WeaponSlot MapTo(gear_slot_t slot);

    gear_slot_t MapTo(WeaponSlot slot);
}