using Common.Effects.Effects.Contracts;
using Common.Effects.Events;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Common.Effects.Effects;

internal sealed class Burn(ISwiftlyCore core, IEventPublisher eventPublisher, IPlayer? caster, IPlayer target) : BaseTickEffect(core, eventPublisher, caster, target)
{
    private const float DamagePerTickInPercent = 1.0f;
    private const float InstantDamageInPercent = 5.0f;
    private const string ParticleName = "particles/inferno_fx/molotov_child_flame01a.vpcf";
    public override float Duration => 5.0f;
    public override void Destroy()
    {
        DestroyEffect();
    }

    public override float TickInterval { get; set; } = 0.5f;
    
    protected override bool CanApply()
    {
        if (!Target.IsValid || !Target.IsAlive) return false;

        return true;
    }

    protected override void ApplyEffect()
    {
        CreateParticle();

        ApplyInstantDamage();
    }

    protected override void DestroyEffect()
    {
        DestroyParticle();
        base.DestroyEffect();
    }

    protected override void TickEffect()
    {
        Target.PlayerPawn?.TakeDamage(
            GetFireDamage(DamagePerTickInPercent),
            DamageTypes_t.DMG_ACID,
            inflictor: null,
            attacker: Caster?.PlayerPawn
        );
    }

    private int GetFireDamage(float percent)
    {
        return (int)(Target.PlayerPawn!.MaxHealth * (percent / 100));
    }

    public override void CreateParticle()
    {
        var playerPawn = Target?.PlayerPawn;
        if (playerPawn == null)
        {
            return;
        }

        Particle = Core.EntitySystem.CreateEntity<CParticleSystem>();
        Particle.EffectName = ParticleName;
        Particle.StartActive = true;
        Particle.DispatchSpawn();

        Particle.Teleport(playerPawn.AbsOrigin, null, null);
        Particle.AcceptInput("SetParent", "!activator", playerPawn, Particle);
        Particle.AcceptInput("SetParentAttachment", "knife", playerPawn);
    }

    public override void DestroyParticle()
    {
        if (Particle != null && Particle.IsValidEntity)
        {
            Particle.Despawn();
        }
    }

    private void ApplyInstantDamage()
    {
        Target.PlayerPawn?.TakeDamage(
            GetFireDamage(InstantDamageInPercent),
            DamageTypes_t.DMG_ACID,
            inflictor: null,
            attacker: Caster?.PlayerPawn
        );
    }
}