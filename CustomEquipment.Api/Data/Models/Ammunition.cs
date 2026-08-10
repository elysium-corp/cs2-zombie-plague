namespace CustomEquipment.Data.Equipments.Models;

public sealed class Ammunition
{
    // - патрон в обойме
    public int? Clip { get; init; }

    // - число обойм
    public int? ReserveAmmo { get; init; }
}