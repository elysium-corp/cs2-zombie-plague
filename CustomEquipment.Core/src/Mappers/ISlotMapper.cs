using CustomEquipment.Data.Equipments.Enums;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Mappers;

internal interface ISlotMapper
{
    Slot MapTo(gear_slot_t slot);

    gear_slot_t MapTo(Slot slot);
}