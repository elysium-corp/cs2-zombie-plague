using Common.Effects;
using Common.Effects.Effects;
using Localization.Api;
using Microsoft.Extensions.Logging;
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
        StopTrap();
        var trap = new TrapEntity(core, config, Caster);
        _trapEntity = trap;
        try
        {
            if (!trap.TrySpawn())
            {
                StopTrap();
                return;
            }

            base.Use();
        }
        catch
        {
            StopTrap();
            throw;
        }
    }

    public override void UnHook()
    {
        try
        {
            StopTrap();
        }
        finally
        {
            base.UnHook();
        }
    }

    private void StopTrap()
    {
        var trap = _trapEntity;
        _trapEntity = null;
        trap?.Dispose();
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
    private readonly List<TrapFreeze> _freezes = [];
    private int _disposed;

    private const float Delay = 0.1f;

    public bool TrySpawn()
    {
        if (_disposed != 0 || Entity != null || caster is not { IsValid: true, IsAlive: true }) return false;

        var playerPawn = caster.PlayerPawn;

        if (playerPawn is not { IsValid: true }) return false;

        Entity = core.EntitySystem.CreateEntity<CParticleSystem>();

        Entity.Render = new Color(255, 255, 255, 0);
        Entity.RenderUpdated();

        Entity.DispatchSpawn();

        Entity.Teleport(playerPawn.AbsOrigin, null, null);

        core.Scheduler.NextWorldUpdate(() =>
        {
            if (_disposed != 0 || Entity == null || !Entity.IsValidEntity) return;

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
        var entity = Entity;
        Entity = null;
        try
        {
            CancelTimer(ref _triggerTask);
        }
        finally
        {
            try
            {
                CancelTimer(ref _despawnTask);
            }
            finally
            {
                if (entity is { IsValidEntity: true }) entity.Despawn();
            }
        }
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
        if (_disposed != 0) return;

        if (Entity == null || !Entity.IsValidEntity || Entity.AbsOrigin == null)
        {
            Despawn();
            return;
        }

        if (!caster.IsValid || !caster.IsAlive)
        {
            Dispose();
            return;
        }

        var foundPlayers = MathAlgorithm.FindAllPlayersInSphere(
            config.TriggerRadius,
            Entity.AbsOrigin.Value
        ).Where(foundPlayer =>
            foundPlayer.IsValid && foundPlayer.IsAlive && foundPlayer.PlayerID != caster.PlayerID &&
            foundPlayer.Controller.Team != caster.Controller.Team).ToList();

        if (foundPlayers.Count > 0)
        {
            try
            {
                foreach (var player in foundPlayers) Trap(player);
            }
            finally
            {
                // Визуальная ловушка исчезает сразу, а её эффекты остаются
                // учтёнными до тайм-аута или снятия способности владельца.
                Despawn();
            }
        }
    }

    private void Trap(IPlayer target)
    {
        if (target is not { IsValid: true, IsAlive: true }) return;

        var targetPawn = target.PlayerPawn;

        if (targetPawn == null || !targetPawn.IsValid) return;

        // Не сохраняем MOVETYPE_NONE как исходное состояние, иначе вложенные
        // ловушки могут оставить цель обездвиженной после снятия эффектов.
        if (targetPawn.MoveType == MoveType_t.MOVETYPE_NONE ||
            targetPawn.ActualMoveType == MoveType_t.MOVETYPE_NONE) return;
        
        var effectService = EffectService.Provide(core);
        var reference = new PlayerPawnReference(target.SessionId, core.EntitySystem.GetRefEHandle(targetPawn).Raw);
        var freeze = new TrapFreeze(core, reference, targetPawn.MoveType, targetPawn.ActualMoveType,
            finished => _freezes.Remove(finished));
        _freezes.Add(freeze);
        try
        {
            targetPawn.MoveType = MoveType_t.MOVETYPE_NONE;
            targetPawn.ActualMoveType = MoveType_t.MOVETYPE_NONE;
            targetPawn.MoveTypeUpdated();
            targetPawn.AbsVelocity = Vector.Zero;

            freeze.Schedule(config.EffectDuration);
            freeze.Disorientation = effectService.ApplyEffect<Disorient>(caster, target);
        }
        catch
        {
            freeze.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try
        {
            Despawn();
        }
        catch (Exception exception)
        {
            core.Logger.LogWarning(exception, "Не удалось удалить визуальную сущность ловушки");
        }

        foreach (var freeze in _freezes.ToArray())
        {
            try
            {
                freeze.Dispose();
            }
            catch (Exception exception)
            {
                core.Logger.LogWarning(exception, "Не удалось завершить эффект ловушки");
            }
        }
        _freezes.Clear();
    }

    private static void CancelTimer(ref CancellationTokenSource? source)
    {
        var timer = source;
        source = null;
        try
        {
            timer?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Отмена не должна мешать очистке остальных ресурсов ловушки.
        }
    }
}
