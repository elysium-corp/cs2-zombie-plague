using CustomEquipment.Data.Equipments.Enums;

namespace CustomEquipment.Data.Equipments.Contracts;

public interface IGrenade : IItem
{
    string InheritorName { get; }
    
    WeaponType WeaponType { get; }
    
    string Model { get; }
}