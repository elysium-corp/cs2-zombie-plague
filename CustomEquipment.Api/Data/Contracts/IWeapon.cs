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

    /// <summary>Параметры отдачи; null сохраняет исходные значения подкласса оружия.</summary>
    WeaponRecoil? WeaponRecoil => null;

    /// <summary>Параметры разброса и неточности; null сохраняет исходные значения подкласса.</summary>
    WeaponAccuracy? WeaponAccuracy => null;
    
    Ammunition? Ammunition { get; }

    IReadOnlyCollection<WeaponSound> Sounds => Array.Empty<WeaponSound>();
}
