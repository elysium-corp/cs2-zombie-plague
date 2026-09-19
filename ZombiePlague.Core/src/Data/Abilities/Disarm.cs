using Localization.Api;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Trace;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class Disarm(
    ISwiftlyCore core,
    DisarmConfig config,
    Func<ILocalizationApi> localization
) : BaseActiveAbility(core, config, localization)
{
    public override KeyKind? Key => KeyKind.E;

    public override float Cooldown => config.CooldownTime;

    private CancellationTokenSource? _projectileToken;

    private Vector _position = Vector.Zero;
    private Vector _direction = Vector.Zero;

    private float _travelledDistance;

    public override void UnHook()
    {
        try
        {
            StopProjectile();
        }
        finally
        {
            base.UnHook();
        }
    }

    public override void Use()
    {
        StopProjectile();

        var casterPawn = Caster.PlayerPawn;

        if (casterPawn == null || !casterPawn.IsValid)
        {
            return;
        }

        var eyePosition = casterPawn.EyePosition;

        if (eyePosition == null)
        {
            return;
        }

        _direction = MathAlgorithm
            .ForwardFromAngles(casterPawn.EyeAngles)
            .Normalized();

        _position =
            eyePosition.Value +
            _direction * config.SpawnOffset;

        _travelledDistance = config.SpawnOffset;

        base.Use();

        if (Particle is not { IsValidEntity: true })
        {
            StopProjectile();
            return;
        }

        StartProjectileHandler();
    }

    protected override bool CanUse()
    {
        if (!Caster.IsValid)
        {
            return false;
        }

        if (!Caster.IsAlive)
        {
            return false;
        }

        var pawn = Caster.PlayerPawn;

        if (pawn == null || !pawn.IsValid)
        {
            return false;
        }

        return pawn.EyePosition != null;
    }

    public override void CreateParticle()
    {
        DestroyParticle();

        if (string.IsNullOrWhiteSpace(
                config.ParticleProjectileEffectName))
        {
            return;
        }

        CreateTemporaryParticle(
            config.ParticleBurstEffectName,
            _position,
            1.0f
        );

        var particle =
            core.EntitySystem.CreateEntity<CParticleSystem>();

        particle.EffectName =
            config.ParticleProjectileEffectName;

        particle.StartActive = true;

        particle.Teleport(
            _position,
            null,
            null
        );

        particle.DispatchSpawn();

        Particle = particle;
    }

    private void CreateTemporaryParticle(
        string particleName,
        Vector position,
        float lifetime
    )
    {
        if (string.IsNullOrWhiteSpace(particleName))
        {
            return;
        }

        var particle =
            core.EntitySystem.CreateEntity<CParticleSystem>();

        particle.EffectName = particleName;
        particle.StartActive = true;

        particle.Teleport(
            position,
            null,
            null
        );

        particle.DispatchSpawn();

        core.Scheduler.DelayBySeconds(
            lifetime,
            () =>
            {
                if (particle.IsValidEntity)
                {
                    particle.Despawn();
                }
            }
        );
    }

    private void StartProjectileHandler()
    {
        CancellationTokenSource? token = null;

        token = core.Scheduler.RepeatBySeconds(
            config.UpdateIntervalSeconds,
            () =>
            {
                if (ReferenceEquals(_projectileToken, token))
                {
                    ProjectileHandler();
                }
            }
        );

        _projectileToken = token;
    }

    private void ProjectileHandler()
    {
        var particle = Particle;

        if (particle == null || !particle.IsValidEntity)
        {
            StopProjectile();
            return;
        }

        if (!Caster.IsValid || !Caster.IsAlive)
        {
            StopProjectile();
            return;
        }

        var casterPawn = Caster.PlayerPawn;

        if (casterPawn == null || !casterPawn.IsValid)
        {
            StopProjectile();
            return;
        }

        var remainingDistance =
            config.MaxDistance - _travelledDistance;

        if (remainingDistance <= 0f)
        {
            StopProjectile();
            return;
        }

        var stepDistance = Math.Min(
            config.ProjectileSpeed *
            config.UpdateIntervalSeconds,
            remainingDistance
        );

        if (stepDistance <= 0f)
        {
            StopProjectile();
            return;
        }

        var nextPosition =
            _position +
            _direction * stepDistance;

        var trace = core.Trace.TraceShapeLine(
            _position,
            nextPosition,
            new TraceParams
            {
                ObjectQuery =
                    RnQueryObjectSet.AllGameEntities |
                    RnQueryObjectSet.Static,

                InteractWith =
                    MaskTrace.Solid |
                    MaskTrace.Player,

                InteractExclude =
                    MaskTrace.Empty,

                InteractAs =
                    MaskTrace.Empty,

                EntitiesToIgnore =
                    [casterPawn]
            }
        );

        if (trace.DidHit)
        {
            var movement =
                nextPosition - _position;

            var hitPosition =
                _position +
                movement * trace.Fraction;

            CreateTemporaryParticle(
                config.ParticleExplodeEffectName,
                hitPosition,
                1.5f
            );

            TryDisarmTarget(trace.Entity);

            StopProjectile();
            return;
        }

        _position = nextPosition;
        _travelledDistance += stepDistance;

        particle.Teleport(
            _position,
            null,
            null
        );

        if (_travelledDistance >= config.MaxDistance)
        {
            StopProjectile();
        }
    }

    private void TryDisarmTarget(
        CEntityInstance? entity
    )
    {
        if (entity == null || !entity.IsValid)
        {
            return;
        }

        var target =
            entity.Address.FindPlayerByPawnAddress();

        if (
            target == null ||
            !target.IsValid ||
            !target.IsAlive
        )
        {
            return;
        }

        if (target.Controller.Team == Caster.Controller.Team)
        {
            return;
        }

        var targetPawn =
            target.PlayerPawn;

        var weaponServices =
            targetPawn?.WeaponServices;

        if (
            targetPawn == null ||
            !targetPawn.IsValid ||
            weaponServices == null
        )
        {
            return;
        }

        var activeWeapon =
            weaponServices.ActiveWeapon.Value?.As<CCSWeaponBase>();

        if (
            activeWeapon == null ||
            !activeWeapon.IsValidEntity
        )
        {
            return;
        }

        var gearSlot =
            activeWeapon.WeaponBaseVData.GearSlot;

        if (
            gearSlot != gear_slot_t.GEAR_SLOT_RIFLE &&
            gearSlot != gear_slot_t.GEAR_SLOT_PISTOL
        )
        {
            return;
        }

        var weaponName =
            activeWeapon.DesignerName;

        if (string.IsNullOrWhiteSpace(weaponName))
        {
            return;
        }

        weaponServices.DropWeaponByDesignerName(
            weaponName
        );

        var throwDirection = MathAlgorithm
            .ForwardFromAngles(targetPawn.EyeAngles)
            .Normalized();

        var throwVelocity = new Vector(
            throwDirection.X * config.ThrowForce,
            throwDirection.Y * config.ThrowForce,
            config.ThrowUpForce
        );

        core.Scheduler.NextWorldUpdate(() =>
        {
            if (
                !activeWeapon.IsValidEntity ||
                activeWeapon.OwnerEntity.IsValid
            )
            {
                return;
            }

            activeWeapon.Teleport(
                null,
                null,
                throwVelocity
            );
        });
    }

    private void StopProjectile()
    {
        var token = _projectileToken;

        _projectileToken = null;

        try
        {
            token?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            DestroyParticle();
        }
    }
}