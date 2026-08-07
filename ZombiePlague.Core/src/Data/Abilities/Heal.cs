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

internal sealed class Heal(ISwiftlyCore core, HealConfig config) : BaseActiveAbility(core, config)
{
    public override KeyKind? Key => KeyKind.E;

    public override float Cooldown => config.CooldownTime;

    private const float EyePositionZ = 64f;

    private CancellationTokenSource? _particleDestroyToken;

    public override void Use()
    {
        var casterPawn = Caster.RequiredPlayerPawn;

        var origin = casterPawn.AbsOrigin;
        if (origin is null)
        {
            return;
        }

        var forward = MathAlgorithm.ForwardFromAngles(casterPawn.EyeAngles);
        var start = origin.Value + new Vector(0f, 0f, EyePositionZ) + forward * 50;
        var end = start + forward * config.MaxHealDistance;

        if (!TryFindHealTarget(casterPawn, start, end, out var target))
        {
            return;
        }

        if (target.Controller.Team != Caster.Controller.Team)
        {
            return;
        }

        Target = target;
        ApplyHeal(target.RequiredPlayerPawn);

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

        var found = entity.Address.FindPlayerByPawnAddress();
        if (found is null || !found.IsValid || !found.Controller.PawnIsAlive)
            return false;

        target = found;
        return true;
    }

    private void ApplyHeal(CBasePlayerPawn targetPawn)
    {
        var maxHealth = Math.Max(targetPawn.MaxHealth, 1);

        var health = Math.Clamp(
            (long)targetPawn.Health + config.HealAmount,
            1L,
            maxHealth
        );

        targetPawn.Health = (int)health;
        targetPawn.HealthUpdated();
    }

    public override void CreateParticle()
    {
        DestroyParticle();

        if (
            Target is not { IsValid: true } target ||
            target.PlayerPawn is not { IsValid: true } pawn ||
            config.ParticleEffectNames.Count == 0
        )
        {
            return;
        }

        var particleEffectName = config.ParticleEffectNames[
            Random.Shared.Next(config.ParticleEffectNames.Count)
        ];

        if (string.IsNullOrWhiteSpace(particleEffectName))
        {
            return;
        }

        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();
        particle.EffectName = particleEffectName;
        particle.StartActive = true;
        particle.DispatchSpawn();

        particle.Teleport(pawn.AbsOrigin, null, null);
        particle.AcceptInput("SetParent", "!activator", pawn, particle);
        particle.AcceptInput("SetParentAttachment", "knife", pawn);

        Particle = particle;

        if (config.HasScreenEffectAfterAbilityOnTarget)
        {
            core.NetMessage.SendCUserMessageFade(
                playerId: target.PlayerID,
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

        _particleDestroyToken = core.Scheduler.DelayBySeconds(
            config.DurationParticleEffect,
            DestroyParticle
        );
    }

    public override void DestroyParticle()
    {
        try
        {
            _particleDestroyToken?.Cancel();
        }
        catch
        {
            // Таймер мог уже завершиться или быть отменён scheduler'ом.
        }
        finally
        {
            _particleDestroyToken = null;
        }

        base.DestroyParticle();
    }

    public override void PlaySound()
    {
        if (config.SoundEffectNames.Count == 0)
        {
            return;
        }

        var soundName = config.SoundEffectNames[
            Random.Shared.Next(config.SoundEffectNames.Count)
        ];

        if (string.IsNullOrWhiteSpace(soundName))
        {
            return;
        }

        using var sound = new SoundEvent(soundName);

        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = (int)Caster.RequiredPlayerPawn.Index;

        sound.Emit();
    }
}