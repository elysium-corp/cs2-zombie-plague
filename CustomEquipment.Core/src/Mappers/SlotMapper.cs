using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Mappers;

internal sealed class SlotMapper : ISlotMapper
{
    public Slot MapTo(gear_slot_t slot)
    {
        return slot switch
        {
            gear_slot_t.GEAR_SLOT_RIFLE => Slot.Primary,
            gear_slot_t.GEAR_SLOT_PISTOL => Slot.Secondary,
            gear_slot_t.GEAR_SLOT_KNIFE => Slot.Knife,
            gear_slot_t.GEAR_SLOT_GRENADES => Slot.Grenade,
            gear_slot_t.GEAR_SLOT_C4 => Slot.Equipment,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }

    public gear_slot_t MapTo(Slot slot)
    {
        return slot switch
        {
            Slot.Primary => gear_slot_t.GEAR_SLOT_RIFLE,
            Slot.Secondary => gear_slot_t.GEAR_SLOT_PISTOL,
            Slot.Knife => gear_slot_t.GEAR_SLOT_KNIFE,
            Slot.Grenade => gear_slot_t.GEAR_SLOT_GRENADES,
            Slot.Equipment => gear_slot_t.GEAR_SLOT_C4,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }
}
