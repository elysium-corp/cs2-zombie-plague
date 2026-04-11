using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZombiePlague.Core.Data.Abilities.Contracts;

internal interface IParticleRestricted
{
    public CParticleSystem? Particle { get; set; }

    public void DestroyParticle();

    void CreateParticle();
}