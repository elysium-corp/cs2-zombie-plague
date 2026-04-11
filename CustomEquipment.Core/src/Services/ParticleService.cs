using CustomEquipment.Controllers;
using CustomEquipment.Data.Equipments.Enums;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Services;

internal sealed class ParticleService(ISwiftlyCore core) : IParticleService
{
    public CParticleSystem CreateParticle(string particleName, Vector pos)
    {
        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();
        
        particle.StartActive = true;
        particle.EffectName = particleName;
        
        particle.Teleport(pos, null, null);
        
        particle.DispatchSpawn();

        return particle;
    }

    public ParticleContext CreateParticle(string particleName, Vector pos, float lifetime)
    {
        var particle = CreateParticle(particleName, pos);

        var token = core.Scheduler.DelayBySeconds(lifetime, () =>
        {
            particle.Despawn();
        });

        return new ParticleContext(particle, token);
    }
    
    public CParticleSystem CreateParticleByControlPoints(string particleName, Vector start, Vector end)
    {
        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();
        
        particle.StartActive = true;
        particle.EffectName = particleName;
        
        particle.Teleport(start, null, null);
        
        particle.ServerControlPoints[0] = start;
        particle.ServerControlPoints[1] = end;

        particle.ServerControlPointAssignments[0] = 0;
        particle.ServerControlPointAssignments[1] = 1;

        particle.DispatchSpawn();

        particle.ServerControlPointsUpdated();
        particle.ServerControlPointAssignmentsUpdated();

        return particle;
    }

    public ParticleContext CreateParticleByControlPoints(string particleName, Vector start, Vector end, float lifetime)
    {
        var particle = CreateParticleByControlPoints(particleName, start, end);

        var token = core.Scheduler.DelayBySeconds(lifetime, () =>
        {
            particle.Despawn();
        });

        return new ParticleContext(particle, token);
    }
    
    public CParticleSystem CreateTracerParticle<TWeapon>(string particleName, TWeapon entity, Vector end) where TWeapon : CCSWeaponBase
    {
        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();
        
        particle.StartActive = true;
        particle.EffectName = particleName;

        var start = ResolveTracerStartFromOwner(entity);
        
        particle.Teleport(start, null, null);
        
        particle.ServerControlPoints[0] = start;
        particle.ServerControlPoints[1] = end;

        particle.ServerControlPointAssignments[0] = 0;
        particle.ServerControlPointAssignments[1] = 1;

        particle.DispatchSpawn();

        particle.ServerControlPointsUpdated();
        particle.ServerControlPointAssignmentsUpdated();

        return particle;
    }

    public ParticleContext CreateTracerParticle<TWeapon>(string particleName,
        TWeapon entity, Vector end, float lifetime) where TWeapon : CCSWeaponBase
    {
        var particle = CreateTracerParticle(particleName, entity, end);

        var token = core.Scheduler.DelayBySeconds(lifetime, () =>
        {
            particle.Despawn();
        });

        return new ParticleContext(particle, token);
    }

    public CParticleSystem CreateParticleAttached(string particleName, CEntityInstance entity, Attachment attachment)
    {
        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();
        
        particle.StartActive = true;
        particle.EffectName = particleName;
        particle.SetParent(entity);
        particle.SetParentAttachment(entity, attachment);
        particle.Teleport(Vector.Zero, null, null);
        
        particle.DispatchSpawn();

        return particle;
    }

    public ParticleContext CreateParticleAttached(string particleName, CEntityInstance entity, Attachment attachment,
        float lifetime)
    {
        var particle = CreateParticleAttached(particleName, entity, attachment);

        var token = core.Scheduler.DelayBySeconds(lifetime, () =>
        {
            particle.Despawn();
        });

        return new ParticleContext(particle, token);
    }
    
    private Vector ResolveTracerStartFromOwner(CCSWeaponBase weapon)
    {
        var owner = weapon.OwnerEntity.Value!.As<CCSPlayerPawn>();
        var eyePosition = owner.EyePosition!.Value;
        var eyeAngles = owner.EyeAngles;

        eyeAngles.ToDirectionVectors(out var forward, out var right, out var up);

        return eyePosition
               + forward * 14.0f
               + right   * 4.0f
               - up      * 2.0f;
    }
}
