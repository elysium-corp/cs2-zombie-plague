using CustomEquipment.Data.Equipments.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.Equipments.Contracts;

public interface IWeapon : IItem
{
    string InheritorName { get; }
    
    WeaponType WeaponType { get; }
    
    string Model { get; }
    
    WeaponDamage? WeaponDamage { get; }
    
    WeaponTiming? WeaponTiming { get; }
    
    Ammunition? Ammunition { get; }
}