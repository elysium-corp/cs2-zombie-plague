using Common.Effects.Effects.Contracts;
using Common.Effects.Effects.Settings;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Common.Effects.Effects;

public sealed class Burn(
    ISwiftlyCore core,
    Action<IEffect> callback,
    IPlayer? caster,
    IPlayer target,
    BurnSettings? settings
) : BaseTickEffect(core, callback, caster, target)
{
    /// <summary>Идентификатор урона горения для обработчиков игровых режимов.</summary>
    public const uint DamageCustomId = 0x4255524E;

    private const string ParticleName =
        "particles/inferno_fx/molotov_child_flame01a.vpcf";

    public BurnSettings Settings { get; } =
        settings ?? new BurnSettings();

    public override float Duration =>
        Settings.Duration;

    protected override float TickInterval =>
        0.5f;

    public override void Destroy()
    {
        DestroyEffect();
    }

    protected override bool CanApply()
    {
        return
            Target.IsValid &&
            Target.IsAlive &&
            Target.PlayerPawn is
            {
                IsValid: true
            };
    }

    protected override void ApplyEffect()
    {
        CreateParticle();

        ApplyBurnDamage(
            Settings.InstantDamageInPercent
        );
    }

    protected override void TickEffect()
    {
        ApplyBurnDamage(
            Settings.DamagePerTickInPercent
        );
    }

    protected override void DestroyEffect()
    {
        DestroyParticle();

        base.DestroyEffect();
    }

    private void ApplyBurnDamage(
        float percent
    )
    {
        if (percent <= 0f)
        {
            return;
        }

        var targetPawn =
            Target.PlayerPawn;

        if (
            targetPawn == null ||
            !targetPawn.IsValid
        )
        {
            return;
        }

        var damage =
            GetFireDamage(
                targetPawn,
                percent
            );

        if (damage <= 0)
        {
            return;
        }
        
        var casterPawn =
            Caster?.PlayerPawn;

        var attacker = casterPawn is { IsValid: true } ? casterPawn : null;
        var damageInfo = new CTakeDamageInfo(
            damage,
            DamageTypes_t.DMG_BURN,
            inflictor: attacker ?? targetPawn,
            attacker: attacker
        )
        {
            DamageCustom = DamageCustomId
        };

        targetPawn.TakeDamage(damageInfo);
    }

    private static int GetFireDamage(
        CCSPlayerPawn targetPawn,
        float percent
    )
    {
        if (targetPawn.MaxHealth <= 0)
        {
            return 0;
        }

        return Math.Max(
            1,
            (int)MathF.Round(
                targetPawn.MaxHealth *
                (percent / 100f)
            )
        );
    }

    protected override void CreateParticle()
    {
        var playerPawn =
            Target.PlayerPawn;

        if (
            playerPawn == null ||
            !playerPawn.IsValid
        )
        {
            return;
        }

        Particle =
            Core.EntitySystem
                .CreateEntity<CParticleSystem>();

        Particle.EffectName =
            ParticleName;

        Particle.StartActive =
            true;

        Particle.DispatchSpawn();

        Particle.Teleport(
            playerPawn.AbsOrigin,
            null,
            null
        );

        Particle.AcceptInput(
            "SetParent",
            "!activator",
            playerPawn,
            Particle
        );

        Particle.AcceptInput(
            "SetParentAttachment",
            "knife",
            playerPawn
        );
    }

    protected override void DestroyParticle()
    {
        if (
            Particle != null &&
            Particle.IsValidEntity
        )
        {
            Particle.Despawn();
        }

        Particle = null;
    }
}
