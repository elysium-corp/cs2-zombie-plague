using Common.Effects;
using Common.Effects.Effects;
using Localization.Api;
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

internal sealed class Trap(ISwiftlyCore core, TrapConfig config, Func<ILocalizationApi> localization)
    : BaseActiveAbility(core, config, localization)
{
    public override KeyKind? Key => KeyKind.E;

    public override float Cooldown => config.CooldownTime;

    private TrapEntity? _trapEntity;

    public override void Use()
    {
        _trapEntity?.Dispose();
        var trap = new TrapEntity(core, config, Caster);

        if (!trap.TrySpawn()) return;

        _trapEntity = trap;

        base.Use();
    }

    public override void UnHook()
    {
        _trapEntity?.Dispose();
        _trapEntity = null;
        base.UnHook();
    }

    protected override bool CanUse()
    {
        if (!Caster.IsValid || !Caster.IsAlive) return false;

        if (Caster.PlayerPawn?.GroundEntity.Value == null) return false;

        return _trapEntity?.Entity == null;
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

        if (string.IsNullOrWhiteSpace(soundName)) return;

        var trapEntity = _trapEntity?.Entity;

        if (trapEntity == null) return;

        var index = (int)trapEntity.Index;

        SoundExt.PlayAtEntity(index, soundName, 1f);
    }
}

internal sealed class TrapEntity(ISwiftlyCore core, TrapConfig config, IPlayer caster) : IDisposable
{
    public CParticleSystem? Entity { get; private set; }

    private CancellationTokenSource? _triggerTask;
    private CancellationTokenSource? _despawnTask;
    private int _disposed;

    private const float Delay = 0.1f;

    public bool TrySpawn()
    {
        if (Entity != null) return false;

        var playerPawn = caster.PlayerPawn;

        if (playerPawn == null) return false;

        Entity = core.EntitySystem.CreateEntity<CParticleSystem>();

        Entity.Render = new Color(255, 255, 255, 0);
        Entity.RenderUpdated();

        Entity.DispatchSpawn();

        Entity.Teleport(playerPawn.AbsOrigin, null, null);

        core.Scheduler.NextWorldUpdate(() =>
        {
            if (Entity == null || !Entity.IsValidEntity) return;

            Entity.SetModel(config.ParticleEffectName);
        });

        var filter = new CRecipientFilter(NetChannelBufType_t.BUF_RELIABLE);
        filter.AddRecipient(caster.PlayerID);

        core.Engine.DispatchParticleEffect(
            config.ParticleEffectName,
            ParticleAttachment_t.PATTACH_ABSORIGIN,
            0,
            string.Empty,
            filter,
            resetAllParticlesOnEntity: false,
            splitScreenSlot: 0,
            Entity
        );

        StartTriggerHandler();

        StartDespawnCallback();

        return true;
    }

    private void Despawn()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        if (Entity != null && Entity.IsValidEntity)
        {
            Entity.Despawn();
            Entity = null;
        }

        _triggerTask?.Cancel();
        _triggerTask = null;
        _despawnTask?.Cancel();
        _despawnTask = null;
    }

    private void StartTriggerHandler()
    {
        _triggerTask = core.Scheduler.RepeatBySeconds(Delay, Trigger);
    }

    private void StartDespawnCallback()
    {
        _despawnTask = core.Scheduler.DelayBySeconds(Math.Max(0.1f, config.LiveDuration), Despawn);
    }

    private void Trigger()
    {
        if (Entity == null || !Entity.IsValidEntity || Entity.AbsOrigin == null)
        {
            Despawn();
            return;
        }

        if (!caster.IsValid || !caster.IsZombie())
        {
            Despawn();
            return;
        }

        var foundPlayers = MathAlgorithm.FindAllPlayersInSphere(
            config.TriggerRadius,
            Entity.AbsOrigin.Value
        ).Where(foundPlayer =>
            foundPlayer.IsValid && foundPlayer.PlayerID != caster.PlayerID && !foundPlayer.IsZombie()).ToList();

        if (foundPlayers.Any())
        {
            foreach (var player in foundPlayers)
            {
                Trap(player);
            }

            Despawn();
        }
    }

    private void Trap(IPlayer target)
    {
        var targetPawn = target.PlayerPawn;

        if (targetPawn == null || !targetPawn.IsValid) return;
        
        var effectService = EffectService.Provide(core);
        
        targetPawn.MoveType = MoveType_t.MOVETYPE_NONE;
        targetPawn.ActualMoveType = MoveType_t.MOVETYPE_NONE;
        targetPawn.MoveTypeUpdated();
        
        effectService.ApplyEffect<Disorient>(caster, target);

        targetPawn.AbsVelocity = Vector.Zero;

        core.Scheduler.DelayBySeconds(config.EffectDuration, () =>
        {
            if (!targetPawn.IsValid) return;

            targetPawn.MoveType = MoveType_t.MOVETYPE_WALK;
            targetPawn.ActualMoveType = MoveType_t.MOVETYPE_WALK;
            targetPawn.MoveTypeUpdated();
        });
    }

    public void Dispose() => Despawn();
}
