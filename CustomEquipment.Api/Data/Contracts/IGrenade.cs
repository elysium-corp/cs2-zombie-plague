using CustomEquipment.Api.Enums;

namespace CustomEquipment.Api.Data.Contracts;

public interface IGrenade : IItem
{
    string InheritorName { get; }
    
    WeaponType WeaponType { get; }
    
    string Model { get; }
}