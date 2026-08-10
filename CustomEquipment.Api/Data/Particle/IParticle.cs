namespace CustomEquipment.Data.Equipments.Particle;

internal interface IParticle
{
    string Trace { get; init; }
    
    public string Impact { get; init; }
    
    public string MuzzleFlash { get; init; }
}