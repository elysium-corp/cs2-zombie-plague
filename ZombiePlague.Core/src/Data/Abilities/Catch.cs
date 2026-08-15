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

internal sealed class Catch(ISwiftlyCore core, CatchConfig config) : BaseActiveAbility(core, config)
{
    public override KeyKind? Key => KeyKind.E;

    public override float Cooldown => config.CooldownTime;

    private CancellationTokenSource? _catchToken;
    private CBeam? _catchBeam;
    private Vector _oldPosition;
    private MoveType_t? _targetMoveType;
    private MoveType_t? _targetActualMoveType;

    private const float BodyPositionZ = 48f;
    private const float UpdateIntervalMs = 0.04f;
    private const float MovementTolerance = 1f;

    public override void UnHook()
    {
        CancelCatching();
        base.UnHook();
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
            base.Use();
            return;
        }

        _oldPosition = casterPawn.AbsOrigin!.Value;

        if (!TryFreeze())
        {
            Target = null;

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
        Unfreeze();

        _catchToken?.Cancel();
        _catchToken = null;

        if (_catchBeam != null && _catchBeam.IsValidEntity)
        {
            _catchBeam?.Despawn();
        }

        _catchBeam = null;
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
        _catchToken = core.Scheduler.RepeatBySeconds(UpdateIntervalMs, CatchHandler);
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
        if(_catchBeam == null || !_catchBeam.IsValidEntity) return false;
        
        if (!Caster.IsValid || !Caster.IsAlive) return false;

        var casterPawn = Caster.PlayerPawn;

        if (casterPawn == null || !casterPawn.IsValid) return false;
        
        if ((casterPawn.MovementServices?.Buttons.ButtonPressed & GameButtonFlags.E) == 0) return false;
        
        if (HasMoved(casterPawn.AbsOrigin!.Value)) return false;
    
        if (Target == null || !Target.IsValid || !Target.IsAlive) return false;

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

        _catchBeam.EndPos = targetPosition.Value + new Vector(0f, 0f, BodyPositionZ);
        _catchBeam.EndPosUpdated();
    }

    private bool TryFreeze()
    {
        var targetPawn = Target?.PlayerPawn;

        if (targetPawn == null || !targetPawn.IsValid) return false;

        _targetMoveType = targetPawn.MoveType;
        _targetActualMoveType = targetPawn.ActualMoveType;

        targetPawn.MoveType = MoveType_t.MOVETYPE_FLYGRAVITY;
        targetPawn.ActualMoveType = MoveType_t.MOVETYPE_FLYGRAVITY;
        targetPawn.MoveTypeUpdated();

        targetPawn.AbsVelocity = Vector.Zero;

        return true;
    }

    private void Unfreeze()
    {
        var targetPawn = Target?.PlayerPawn;

        if (targetPawn == null || !targetPawn.IsValid) return;

        targetPawn.MoveType = _targetMoveType ?? MoveType_t.MOVETYPE_WALK;
        targetPawn.ActualMoveType = _targetActualMoveType ?? MoveType_t.MOVETYPE_WALK;
        targetPawn.MoveTypeUpdated();

        _targetMoveType = null;
        _targetActualMoveType = null;
    }

    private bool TryCatchTarget()
    {
        var casterPosition = Caster.PlayerPawn?.AbsOrigin;
        var targetPawn = Target?.PlayerPawn;
        var targetPosition = targetPawn?.AbsOrigin;

        if (casterPosition == null || targetPawn == null || !targetPawn.IsValid || targetPosition == null)
        {
            return false;
        }

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