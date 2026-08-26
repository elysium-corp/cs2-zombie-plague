using CustomEquipment.Api.Data;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Trace;

namespace CustomEquipment.Data.Equipments.Weapons.Equipments.Entities;

public sealed class LaserMineEntity(ISwiftlyCore core) : LaserMineEntityBase(core)
{
    public override string LaserMineModel => "models/lasermine.vmdl";
    public override float TriggerInterval => 0.15f;

    private const string DamageParticle = "particles/explosions_fx/bumpmine_detonate_sparks.vpcf";
    private const float DamagePerTrigger = 35f;
    private const DamageTypes_t DamageType = DamageTypes_t.DMG_POISON;

    protected override void Trigger()
    {
        if (LaserMine == null || LaserMineTracer == null || !LaserMine.IsValidEntity || !LaserMineTracer.IsValidEntity)
        {
            Destroy();
            return;
        }

        if (Owner == null || Owner.PlayerPawn?.Team == Team.T)
        {
            Destroy();
            return;
        }

        var foundTarget = TryFindTarget(out var target, out var hitPoint);

        UpdateTracer(hitPoint);

        if (!foundTarget) return;

        ApplyDamage(target);

        // CreateDamageParticle(hitPoint);
    }

    private bool TryFindTarget(out IPlayer target, out Vector hitPoint)
    {
        target = null!;
        hitPoint = default;

        if (LaserMine!.AbsRotation == null) return false;

        var forward = ForwardFromAngles(LaserMine.AbsRotation.Value);
        var start = LaserMine.AbsOrigin;

        if (start == null) return false;

        var end = start + forward * TracerDistance;

        if (end == null) return false;

        var trace = core.Trace.TraceShapeLine(
            start.Value,
            end.Value,
            new TraceParams
            {
                ObjectQuery = RnQueryObjectSet.AllGameEntities | RnQueryObjectSet.Static,
                InteractWith = MaskTrace.Solid | MaskTrace.Player,
                InteractExclude = MaskTrace.Empty,
                InteractAs = MaskTrace.Empty,
                EntitiesToIgnore = [LaserMine]
            }
        );

        hitPoint = trace.EndPos;

        var entity = trace.Entity;
        if (entity is null) return false;

        var found = entity.Address.FindPlayerByPawnAddress();

        if (found is null || !found.IsValid || !found.IsAlive) return false;

        target = found;
        return true;
    }

    private void ApplyDamage(IPlayer target)
    {
        if (target.PlayerPawn?.Team == Team.T)
            target.PlayerPawn?.TakeDamage(DamagePerTrigger, DamageType, LaserMine);
    }

    private void UpdateTracer(Vector hitPoint)
    {
        LaserMineTracer?.EndPos = hitPoint == default ? LaserDirection : hitPoint;
        LaserMineTracer?.EndPosUpdated();
    }

    private void CreateDamageParticle(Vector hitPoint)
    {
        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();

        particle.EffectName = DamageParticle;
        particle.StartActive = true;
        particle.DispatchSpawn();

        particle.Teleport(hitPoint, null, null);
    }
}