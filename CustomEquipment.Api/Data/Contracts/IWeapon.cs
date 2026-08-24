using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Api.Data.Contracts;

public interface IWeapon : IItem
{
    string InheritorName { get; }
    
    WeaponType WeaponType { get; }
    
    string Model { get; }
    
    WeaponDamage? WeaponDamage { get; }
    
    WeaponTiming? WeaponTiming { get; }
    
    Ammunition? Ammunition { get; }

    IReadOnlyCollection<WeaponSound> Sounds => Array.Empty<WeaponSound>();
}
