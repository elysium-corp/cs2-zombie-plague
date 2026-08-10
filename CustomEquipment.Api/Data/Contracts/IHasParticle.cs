using CustomEquipment.Api.Data.Models;

namespace CustomEquipment.Api.Data.Contracts;

internal interface IHasParticle
{
    WeaponParticle? Particle { get; }
}