using CustomEquipment.Api.Events;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentEvents(
    CustomEquipmentItemEvents items,
    CustomEquipmentWeaponEvents weapons,
    CustomEquipmentGrenadeEvents grenades,
    CustomEquipmentMineEvents mines
) : ICustomEquipmentEvents
{
    public ICustomEquipmentItemEvents Items => items;

    public ICustomEquipmentWeaponEvents Weapons => weapons;

    public ICustomEquipmentGrenadeEvents Grenades => grenades;

    public ICustomEquipmentMineEvents Mines => mines;
}
