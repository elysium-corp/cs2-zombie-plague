using CustomEquipment.Data.Equipments.Models;
using CustomEquipment.Data.Equipments.Particle;

namespace CustomEquipment.Data.Equipments.Contracts;

internal interface IHasParticle
{
    WeaponParticle? Particle { get; }
}