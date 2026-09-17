using Common.Effects;
using Common.Effects.Effects;
using Common.Effects.Effects.Settings;
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

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class FireBomb(
    ISwiftlyCore core,
    FireBombConfig config,
    Func<ILocalizationApi> localization
) : BaseActiveAbility(core, config, localization)
{
    public override KeyKind? Key => KeyKind.E;

    public override float Cooldown => config.CooldownTime;

    private CancellationTokenSource? _projectileToken;

    private Vector _position = Vector.Zero;
    private Vector _velocity = Vector.Zero;

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

        if (
            casterPawn == null ||
            !casterPawn.IsValid ||
            casterPawn.EyePosition == null
        )
        {
            return;
        }
        
        var forward = MathAlgorithm
            .ForwardFromAngles(casterPawn.EyeAngles)
            .Normalized();

        _position =
            casterPawn.EyePosition.Value +
            forward * config.SpawnOffset;
        
        _velocity =
            forward *
            config.ProjectileSpeed;

        _travelledDistance = 0f;
        
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

        if (
            pawn == null ||
            !pawn.IsValid ||
            pawn.EyePosition == null
        )
        {
            return false;
        }

        return true;
    }

    public override void CreateParticle()
    {
        DestroyParticle();

        if (string.IsNullOrWhiteSpace(
                config.ParticleProjectileEffectName))
        {
            return;
        }

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

    private void StartProjectileHandler()
    {
        CancellationTokenSource? token = null;

        token = core.Scheduler.RepeatBySeconds(
            config.UpdateIntervalSeconds,
            () =>
            {
                if (!ReferenceEquals(
                        _projectileToken,
                        token))
                {
                    return;
                }

                ProjectileHandler();
            }
        );

        _projectileToken = token;
    }

    private void ProjectileHandler()
    {
        if (!Caster.IsValid || !Caster.IsAlive)
        {
            StopProjectile();
            return;
        }

        var casterPawn = Caster.PlayerPawn;

        if (
            casterPawn == null ||
            !casterPawn.IsValid
        )
        {
            StopProjectile();
            return;
        }

        if (Particle is not { IsValidEntity: true } particle)
        {
            StopProjectile();
            return;
        }

        var dt =
            config.UpdateIntervalSeconds;
        
        var gravityMovement =
            0.5f *
            config.ProjectileGravity *
            dt *
            dt;

        var nextPosition = new Vector(
            _position.X +
            _velocity.X * dt,

            _position.Y +
            _velocity.Y * dt,

            _position.Z +
            _velocity.Z * dt -
            gravityMovement
        );
        
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
            var impactPosition =
                trace.EndPos;
            
            StopProjectile();

            core.Scheduler.NextWorldUpdate(
                () => Explode(impactPosition)
            );

            return;
        }

        var movement =
            nextPosition -
            _position;

        _travelledDistance +=
            Length(movement);

        _position =
            nextPosition;

        _velocity.Z -=
            config.ProjectileGravity *
            dt;

        particle.Teleport(
            _position,
            null,
            null
        );

        if (
            _travelledDistance >=
            config.MaxDistance
        )
        {
            StopProjectile();
        }
    }

    private void Explode(Vector position)
    {
        if (!Caster.IsValid)
        {
            return;
        }

        CreateExplosionParticle(position);

        var players = core.PlayerManager
            .GetAlive()
            .ToArray();

        foreach (var target in players)
        {
            ApplyExplosionToTarget(
                target,
                position
            );
        }
    }

    private void ApplyExplosionToTarget(
        IPlayer target,
        Vector explosionPosition
    )
    {
        if (!target.IsValid || !target.IsAlive)
        {
            return;
        }
        
        if (
            target.Controller.Team ==
            Caster.Controller.Team
        )
        {
            return;
        }

        var targetPawn = target.PlayerPawn;

        if (
            targetPawn == null ||
            !targetPawn.IsValid ||
            targetPawn.AbsOrigin == null
        )
        {
            return;
        }

        var distance = Distance(
            explosionPosition,
            targetPawn.AbsOrigin.Value
        );

        if (distance > config.ExplosionRadius)
        {
            return;
        }
        
        var factor = Math.Clamp(
            1f -
            distance / config.ExplosionRadius,
            0f,
            1f
        );

        if (factor <= 0f)
        {
            return;
        }

        ApplyKnockback(
            targetPawn,
            explosionPosition,
            factor
        );

        ApplyBurn(
            target,
            factor
        );
    }
    
    private void ApplyBurn(
    IPlayer target,
    float distanceFactor
)
{
    var targetPawn = target.PlayerPawn;

    if (
        targetPawn == null ||
        !targetPawn.IsValid ||
        targetPawn.MaxHealth <= 0
    )
    {
        return;
    }
    
    var totalDamage = Math.Max(
        1,
        (int)MathF.Round(
            config.BurnMaxDamage *
            distanceFactor
        )
    );
    
    const float burnTickInterval = 0.5f;

    var tickCount = Math.Max(
        1,
        (int)MathF.Floor(
            config.BurnDuration /
            burnTickInterval
        )
    );
    
    var damagePerTick =
        totalDamage /
        tickCount;

    var instantDamage =
        totalDamage -
        damagePerTick *
        tickCount;

    var maxHealth =
        targetPawn.MaxHealth;
    
    var damagePerTickPercent =
        damagePerTick <= 0
            ? 0f
            : damagePerTick /
              (float)maxHealth *
              100f;

    var instantDamagePercent =
        instantDamage <= 0
            ? 0f
            : instantDamage /
              (float)maxHealth *
              100f;

    var burnSettings = new BurnSettings(
        config.BurnDuration,
        damagePerTickPercent,
        instantDamagePercent
    );

    var effectService =
        EffectService.Provide(core);
    
    effectService.ApplyEffect<Burn>(
        Caster,
        target,
        burnSettings
    );
}

    private void ApplyKnockback(
        CCSPlayerPawn targetPawn,
        Vector explosionPosition,
        float factor
    )
    {
        if (
            !targetPawn.IsValid ||
            targetPawn.AbsOrigin == null
        )
        {
            return;
        }

        var targetPosition =
            targetPawn.AbsOrigin.Value;

        var offset =
            targetPosition -
            explosionPosition;

        var horizontal =
            new Vector(
                offset.X,
                offset.Y,
                0f
            );

        var horizontalLength =
            Length(horizontal);

        Vector direction;

        if (horizontalLength <= 0.001f)
        {
            direction =
                Vector.Zero;
        }
        else
        {
            direction =
                horizontal *
                (1f / horizontalLength);
        }

        var horizontalPower =
            config.KnockbackPower *
            factor;

        var verticalFactor =
            0.5f +
            factor * 0.5f;

        var verticalPower =
            config.KnockbackUp *
            verticalFactor;

        var currentVelocity =
            targetPawn.AbsVelocity;
        
        var newZ =
            Math.Max(
                currentVelocity.Z + verticalPower,
                verticalPower
            );

        targetPawn.GroundEntity.Value =
            null;

        targetPawn.AbsVelocity =
            new Vector(
                currentVelocity.X +
                direction.X * horizontalPower,

                currentVelocity.Y +
                direction.Y * horizontalPower,

                newZ
            );
    }

    private void CreateExplosionParticle(
        Vector position
    )
    {
        if (string.IsNullOrWhiteSpace(
                config.ParticleExplosionEffectName))
        {
            return;
        }

        var particle =
            core.EntitySystem
                .CreateEntity<CParticleSystem>();

        particle.EffectName =
            config.ParticleExplosionEffectName;

        particle.StartActive =
            true;
        
        particle.Teleport(
            position,
            null,
            null
        );

        particle.DispatchSpawn();

        var lifetime =
            Math.Max(
                0.1f,
                config.ExplosionParticleDuration
            );

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

    private static float Distance(
        Vector first,
        Vector second
    )
    {
        return Length(
            second -
            first
        );
    }

    private static float Length(
        Vector vector
    )
    {
        return MathF.Sqrt(
            vector.X *
            vector.X +

            vector.Y *
            vector.Y +

            vector.Z *
            vector.Z
        );
    }

    private void StopProjectile()
    {
        var token =
            _projectileToken;

        _projectileToken =
            null;

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