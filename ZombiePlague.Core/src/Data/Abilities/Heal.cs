using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class Heal(ISwiftlyCore core, HealConfig config) : BaseActiveAbility(core)
{
    public override KeyKind? Key => KeyKind.E;

    public override float Cooldown => config.CooldownTime;

    private const float EyePositionZ = 64f;
    
    public override void Use()
    {
        var casterPawn = Caster.RequiredPlayerPawn;

        var origin = casterPawn.AbsOrigin;
        if (origin is null)
        {
            return;
        }

        var forward = MathAlgorithm.ForwardFromAngles(casterPawn.EyeAngles);
        var start = origin.Value + new Vector(0f, 0f, EyePositionZ) + forward*50;
        var end = start + forward * config.MaxHealDistance;

        if (!TryFindHealTarget(casterPawn, start, end, out var target))
        {
            return;
        }

        if (!target.IsOnZombieTeam())
        {
            return;
        }

        var targetPawn = target.RequiredPlayerPawn;

        ApplyHeal(target, targetPawn);
        Target = target;

        PlaySound();

        base.Use();
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

        if (!Caster.IsOnZombieTeam())
        {
            return false;
        }

        return true;
    }

    private bool TryFindHealTarget(CCSPlayerPawn casterPawn, Vector start, Vector end, out IPlayer target)
    {
        target = null!;
        
        var trace = new CGameTrace();
        core.Trace.SimpleTrace(
            start,
            end,
            RayType_t.RAY_TYPE_LINE,
            RnQueryObjectSet.AllGameEntities | RnQueryObjectSet.Static,
            MaskTrace.Solid | MaskTrace.Player,
            MaskTrace.Empty,
            MaskTrace.Empty,
            CollisionGroup.Player,
            ref trace,
            casterPawn
        );

        var entity = trace.Entity;
        if (entity is null)
            return false;

        var found = entity.Address.FindPlayerByPawnAddress(core);
        if (found is null || !found.IsValid || !found.Controller.PawnIsAlive)
            return false;

        target = found;
        return true;
    }

    private void ApplyHeal(IPlayer target, CBasePlayerPawn targetPawn)
    {
        var currentHp = targetPawn.Health;
        var newHp = currentHp + config.HealAmount;
        var maxTargetHp = targetPawn.MaxHealth;

        if (newHp >= maxTargetHp)
        {
            newHp = maxTargetHp;
        }

        if (newHp < 1)
        {
            newHp = 1;
        }

        target.SetHealth(newHp);
    }

    public override void CreateParticle()
    {
        DestroyParticle();

        var pawn = Target?.RequiredPlayerPawn;

        if (Target == null)
        {
            return;
        }

        if (pawn == null)
        {
            return;
        }

        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();

        var random = new Random();
        var listOfParticle = config.ParticleEffectNames;
        var randomParticleEffect = config.ParticleEffectNames[random.Next(listOfParticle.Count)];

        particle.EffectName = randomParticleEffect;
        particle.StartActive = true;
        particle.DispatchSpawn();

        particle.Teleport(pawn.AbsOrigin, null, null);
        particle.AcceptInput("SetParent", "!activator", pawn, particle);
        particle.AcceptInput("SetParentAttachment", "knife", pawn);

        Particle = particle;

        if (config.HasScreenEffectAfterAbilityOnTarget)
        {
            core.NetMessage.SendCUserMessageFade(
                playerId: Target.PlayerID,
                duration: config.DurationEffectAfterAbilityOnTarget,
                holdTime: config.HoldTimeEffectAfterAbilityOnTarget,
                flags: NetMessageExt.FFadeIn | NetMessageExt.FFadeOut,
                color: NetMessageExt.Rgba(
                    r: config.RedColorEffectAfterAbilityOnTarget,
                    g: config.GreenColorEffectAfterAbilityOnTarget,
                    b: config.BlueColorEffectAfterAbilityOnTarget,
                    a: config.AlphaEffectAfterAbilityOnTarget
                )
            );
        }

        core.Scheduler.DelayBySeconds(config.DurationParticleEffect, DestroyParticle);
    }

    public override void PlaySound()
    {
        if (config.SoundEffectNames.Count == 0)
        {
            return;
        }

        var randomSound = config.SoundEffectNames[Random.Shared.Next(config.SoundEffectNames.Count)];

        using var sound = new SoundEvent(randomSound);

        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = (int)Caster.RequiredPlayerPawn.Index;

        sound.Emit();
    }
}
