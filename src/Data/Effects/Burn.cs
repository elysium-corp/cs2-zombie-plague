using CS2ZombiePlague.Data.Abilities.Contracts;
using CS2ZombiePlague.Data.Effects.Contracts;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Effects;

public class Burn(IPlayer? caster, IPlayer target) : BaseTickEffect(caster, target), IParticleRestricted
{
    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();
    public override float TickInterval { get; set; } = 1.0f;
    public override float Duration { get; set; } = 5.0f;
    private float DamagePerTickInPercent { get; } = 2.0f;
    public CParticleSystem? Particle { get; set; }
    private const string ParticleName = "particles/burning_fx/env_fire_small_b.vpcf";

    protected override bool CanApply()
    {
        if (!Target.IsValid || !Target.IsAlive) return false;

        return true;
    }

    protected override void ApplyEffect()
    {
        CreateParticle();
    }
    
    protected override void DestroyEffect()
    {
        DestroyParticle();
        base.DestroyEffect();
    }

    protected override void TickEffect()
    {
        Target.PlayerPawn?.TakeDamage(
            GetFireDamage(),
            DamageTypes_t.DMG_ACID,
            inflictor: null,
            attacker: Caster?.PlayerPawn
        );
    }
    private int GetFireDamage()
    {
        return (int)(Target.PlayerPawn!.MaxHealth * (DamagePerTickInPercent / 100));
    }
 
    public void CreateParticle()
    {
        var playerPawn = Target?.PlayerPawn;
        if (playerPawn == null)
        {
            return;
        }
        
        Particle = _core.EntitySystem.CreateEntity<CParticleSystem>();
        Particle.EffectName = ParticleName;
        Particle.StartActive = true;
        Particle.DispatchSpawn();

        Particle.Teleport(playerPawn.AbsOrigin, null, null);
        Particle.AcceptInput("SetParent", "!activator", playerPawn, Particle);
        Particle.AcceptInput("SetParentAttachment", "knife", playerPawn);
    }

    public void DestroyParticle()
    {
        if (Particle != null && Particle.IsValidEntity)
        {
            Particle.Despawn();
        }
    }
}