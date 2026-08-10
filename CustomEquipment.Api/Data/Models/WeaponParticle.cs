using CustomEquipment.Data.Equipments.Particle;

namespace CustomEquipment.Api.Data.Models;

public sealed class WeaponParticle : IParticle
{
    public string Trace { get; init; } = "";
    
    public string Impact { get; init; } = "";
    
    public string MuzzleFlash { get; init; } = "";
}