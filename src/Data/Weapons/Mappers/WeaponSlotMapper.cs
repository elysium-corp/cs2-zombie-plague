using CS2ZombiePlague.Data.Weapons.Enums;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Mappers;

public sealed class WeaponSlotMapper : IWeaponSlotMapper
{
    public WeaponSlot MapTo(gear_slot_t slot)
    {
        return slot switch
        {
            gear_slot_t.GEAR_SLOT_RIFLE => WeaponSlot.Primary,
            gear_slot_t.GEAR_SLOT_PISTOL => WeaponSlot.Secondary,
            gear_slot_t.GEAR_SLOT_KNIFE => WeaponSlot.Knife,
            gear_slot_t.GEAR_SLOT_GRENADES => WeaponSlot.Grenades,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }

    public gear_slot_t MapTo(WeaponSlot slot)
    {
        return slot switch
        {
            WeaponSlot.Primary => gear_slot_t.GEAR_SLOT_RIFLE,
            WeaponSlot.Secondary => gear_slot_t.GEAR_SLOT_PISTOL,
            WeaponSlot.Knife => gear_slot_t.GEAR_SLOT_KNIFE,
            WeaponSlot.Grenades => gear_slot_t.GEAR_SLOT_GRENADES,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }
}

public static class WeaponSlotHelper
{
    private static readonly WeaponSlotMapper Mapper = new();
    
    public static WeaponSlot MapToWeaponSlot(this gear_slot_t slot)
    {
        return Mapper.MapTo(slot);
    }
    
    public static gear_slot_t MapToGearSlot(this WeaponSlot slot)
    {
        return Mapper.MapTo(slot);
    } 
}