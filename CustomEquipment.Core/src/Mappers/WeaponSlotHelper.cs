using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Mappers;

internal static class WeaponSlotHelper
{
    private static readonly SlotMapper Mapper = new();
    
    public static Slot MapToWeaponSlot(this gear_slot_t slot)
    {
        return Mapper.MapTo(slot);
    }
    
    public static gear_slot_t MapToGearSlot(this Slot slot)
    {
        return Mapper.MapTo(slot);
    } 
}