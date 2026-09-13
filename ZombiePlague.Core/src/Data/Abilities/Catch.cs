using Localization.Api;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;
using SwiftlyS2.Shared.Trace;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class Catch(ISwiftlyCore core, CatchConfig config, Func<ILocalizationApi> localization)
    : BaseActiveAbility(core, config, localization)
{
    public override KeyKind? Key => KeyKind.E;

    public override float Cooldown => config.CooldownTime;

    private CancellationTokenSource? _catchToken;
    private CBeam? _catchBeam;
    private Vector _oldPosition;
    private MoveType_t? _targetMoveType;
    private MoveType_t? _targetActualMoveType;
    private uint? _targetPawnHandle;

    private static readonly Vector BodyPositionZ = new(0f, 0f, 48);
    private const float UpdateIntervalSeconds = 0.1f;
    private const float MovementTolerance = 1f;

    public override void UnHook()
    {
        try
        {
            CancelCatching();
        }
        finally
        {
            base.UnHook();
        }
    }

    public override void Use()
    {
        CancelCatching();

        if (!TryFindTarget(out var target))
        {
            SoundExt.PlayAt(Caster, config.MissSound, 1f);
            base.Use();
            return;
        }

        Target = target;

        CreateAndInitializeBeam();

        var casterPawn = Caster.PlayerPawn;

        if (casterPawn == null || !casterPawn.IsValid)
        {
            CancelCatching();
            base.Use();
            return;
        }

        _oldPosition = casterPawn.AbsOrigin!.Value;

        if (!TryFreeze())
        {
            CancelCatching();

            base.Use();

            return;
        }

        CreateCatchingHandler();

        base.Use();
    }

    protected override bool CanUse()
    {
        return Caster.IsValid && Caster.IsAlive;
    }

    private bool TryFindTarget(out IPlayer target)
    {
        target = null!;

        var casterPawn = Caster.PlayerPawn;

        if (casterPawn == null || !casterPawn.IsValid) return false;

        var trace = LaunchTraceFromCaster(casterPawn, casterPawn.EyePosition!.Value);

        var entity = trace.Entity;
        if (entity == null || !entity.IsValid) return false;

        var found = entity.Address.FindPlayerByPawnAddress();
        if (found == null || !found.IsValid || !found.IsAlive) return false;

        target = found;

        return true;
    }

    private void CancelCatching()
    {
        var token = _catchToken;
        var beam = _catchBeam;
        _catchToken = null;
        _catchBeam = null;

        // Сначала останавливаем callback, даже если цель уже отключилась.
        try
        {
            token?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Scheduler мог освободить уже отменённый таймер.
        }
        finally
        {
            try
            {
                Unfreeze();
            }
            finally
            {
                Target = null;
                if (beam is { IsValidEntity: true })
                {
                    beam.Despawn();
                }
            }
        }
    }

    private TraceResult LaunchTraceFromCaster(CCSPlayerPawn casterPawn, Vector start)
    {
        var direction = MathAlgorithm.ForwardFromAngles(casterPawn.EyeAngles);
        var end = start + direction * config.MaxDistance;

        var trace = core.Trace.TraceShapeLine(
            start,
            end,
            new TraceParams
            {
                ObjectQuery = RnQueryObjectSet.AllGameEntities | RnQueryObjectSet.Static,
                InteractWith = MaskTrace.Solid | MaskTrace.Player,
                InteractExclude = MaskTrace.Empty,
                InteractAs = MaskTrace.Empty,
                EntitiesToIgnore = [casterPawn]
            }
        );

        return trace;
    }

    private void CreateAndInitializeBeam()
    {
        var casterPawn = Caster.PlayerPawn;

        if (casterPawn == null || !casterPawn.IsValid) return;

        var targetPawn = Target?.PlayerPawn;

        if (targetPawn == null || !targetPawn.IsValid) return;

        _catchBeam = core.EntitySystem.CreateEntity<CBeam>();
        _catchBeam.Width = config.BeamWidth;
        _catchBeam.Render = new Color(
            config.RedColorEffect,
            config.GreenColorEffect,
            config.BlueColorEffect
        );

        _catchBeam.Teleport(casterPawn.EyePosition, casterPawn.AbsRotation, null);
        _catchBeam.DispatchSpawn();

        _catchBeam.EndPos = targetPawn.AbsOrigin!.Value;
        _catchBeam.EndPosUpdated();
    }

    private void CreateCatchingHandler()
    {
        CancellationTokenSource? token = null;
        token = core.Scheduler.RepeatBySeconds(UpdateIntervalSeconds, () =>
        {
            if (ReferenceEquals(_catchToken, token))
            {
                CatchHandler();
            }
        });
        _catchToken = token;
    }

    private void CatchHandler()
    {
        if (!CanCatch())
        {
            CancelCatching();
            return;
        }

        if (!TryCatchTarget())
        {
            CancelCatching();
            return;
        }

        RefreshCatchingBeam();
    }

    private bool CanCatch()
    {
        if (_catchBeam == null || !_catchBeam.IsValidEntity) return false;

        if (!Caster.IsValid || !Caster.IsAlive) return false;

        var casterPawn = Caster.PlayerPawn;

        if (casterPawn == null || !casterPawn.IsValid) return false;

        if ((casterPawn.MovementServices?.Buttons.ButtonPressed & GameButtonFlags.E) == 0) return false;
        
        if (HasMoved(casterPawn.AbsOrigin!.Value)) return false;
        
        if (Target == null || !Target.IsValid || !Target.IsAlive) return false;

        if (Target.PlayerPawn is not { IsValid: true } targetPawn ||
            core.EntitySystem.GetRefEHandle(targetPawn).Raw != _targetPawnHandle) return false;

        if (Target.Controller.Team == Caster.Controller.Team) return false;

        return true;
    }
    
    private bool HasMoved(Vector currentPosition)
    {
        return
            Math.Abs(_oldPosition.X - currentPosition.X) > MovementTolerance ||
            Math.Abs(_oldPosition.Y - currentPosition.Y) > MovementTolerance ||
            Math.Abs(_oldPosition.Z - currentPosition.Z) > MovementTolerance;
    }

    private void RefreshCatchingBeam()
    {
        var targetPosition = Target?.PlayerPawn?.AbsOrigin;

        if (targetPosition == null) return;

        if (_catchBeam == null || !_catchBeam.IsValidEntity) return;

        _catchBeam.EndPos = targetPosition.Value + BodyPositionZ;
        _catchBeam.EndPosUpdated();
    }

    private bool TryFreeze()
    {
        if (Target is not { IsValid: true, IsAlive: true }) return false;

        var targetPawn = Target?.PlayerPawn;

        if (targetPawn == null || !targetPawn.IsValid) return false;

        _targetMoveType = targetPawn.MoveType;
        _targetActualMoveType = targetPawn.ActualMoveType;
        _targetPawnHandle = core.EntitySystem.GetRefEHandle(targetPawn).Raw;

        targetPawn.MoveType = MoveType_t.MOVETYPE_FLYGRAVITY;
        targetPawn.ActualMoveType = MoveType_t.MOVETYPE_FLYGRAVITY;
        targetPawn.MoveTypeUpdated();

        targetPawn.AbsVelocity = Vector.Zero;

        return true;
    }

    private void Unfreeze()
    {
        var moveType = _targetMoveType;
        var actualMoveType = _targetActualMoveType;
        var pawnHandle = _targetPawnHandle;
        _targetMoveType = null;
        _targetActualMoveType = null;
        _targetPawnHandle = null;

        if (moveType is null || actualMoveType is null || pawnHandle is null) return;

        try
        {
            // Null-проверка не защищает от disposed IPlayer. Новый pawn после
            // респавна также не должен получать параметры предыдущей жизни.
            if (Target is not { IsValid: true, IsAlive: true } ||
                Target.PlayerPawn is not { IsValid: true } targetPawn ||
                core.EntitySystem.GetRefEHandle(targetPawn).Raw != pawnHandle) return;

            targetPawn.MoveType = moveType.Value;
            targetPawn.ActualMoveType = actualMoveType.Value;
            targetPawn.MoveTypeUpdated();
        }
        catch (ObjectDisposedException)
        {
            // Отключившуюся цель больше не нужно размораживать.
        }
    }

    private bool TryCatchTarget()
    {
        var casterPosition = Caster.PlayerPawn?.AbsOrigin + BodyPositionZ;
        var targetPawn = Target?.PlayerPawn;
        var targetPosition = targetPawn?.AbsOrigin;

        if (casterPosition == null || targetPawn == null || !targetPawn.IsValid || targetPosition == null) return false;

        var offset = casterPosition.Value - targetPosition.Value;
        var distanceSquared =
            offset.X * offset.X +
            offset.Y * offset.Y +
            offset.Z * offset.Z;

        if (distanceSquared <= float.Epsilon)
        {
            targetPawn.AbsVelocity = Vector.Zero;
            return true;
        }

        var direction = offset.Normalized();
        targetPawn.AbsVelocity = direction * config.Strength;
        return true;
    }

    public override void PlaySound()
    {
        SoundExt.PlayAt(Caster, config.ShotSound, 1f);
    }
}
