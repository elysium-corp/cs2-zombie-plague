using CustomEquipment.Api.Enums;
using CustomEquipment.Controllers;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Services;

internal interface IParticleService
{
    public CParticleSystem CreateParticle(string particleName, Vector pos);

    public ParticleContext CreateParticle(string particleName, Vector pos, float lifetime);

    public CParticleSystem CreateParticleByControlPoints(string particleName, Vector start, Vector end);

    public ParticleContext CreateParticleByControlPoints(string particleName, Vector start, Vector end, float lifetime);

    public CParticleSystem CreateTracerParticle<TWeapon>(string particleName, TWeapon entity, Vector end)
        where TWeapon : CCSWeaponBase;

    public ParticleContext CreateTracerParticle<TWeapon>(string particleName, TWeapon entity, Vector end,
        float lifetime) where TWeapon : CCSWeaponBase;


    public CParticleSystem CreateParticleAttached(string particleName, CEntityInstance entity, Attachment attachment);

    public ParticleContext CreateParticleAttached(string particleName, CEntityInstance entity, Attachment attachment,
        float lifetime);
}