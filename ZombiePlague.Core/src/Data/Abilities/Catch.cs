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

internal sealed class Catch(ISwiftlyCore core, CatchConfig config) : BaseActiveAbility(core, config)
{
    public override KeyKind? Key => KeyKind.E;

    public override float Cooldown => config.CooldownTime;

    private CBeam? _beam;
    private CancellationTokenSource? _thinker;
    private Vector? _oldPosition;
    private MoveType_t? _targetMoveType;
    private MoveType_t? _targetActualMoveType;

    private const float BodyPositionZ = 48f;
    private const float ThinkerInterval = 0.1f;
    private const float MovementTolerance = 1f;

    public override void Use()
    {
        var casterPawn = Caster.PlayerPawn;
        var casterPosition = casterPawn?.AbsOrigin;
        var casterEyePosition = casterPawn?.EyePosition;

        if (
            casterPawn is not { IsValid: true } ||
            casterPosition is null ||
            casterEyePosition is null
        )
        {
            return;
        }

        CancelCatching();

        var trace = LaunchTraceFromCaster(casterPawn, casterEyePosition.Value);
        var entity = trace.Entity;
        IPlayer? found = null;

        if (entity is not null)
        {
            found = entity.Address.FindPlayerByPawnAddress();
        }

        if (
            found is not { IsValid: true, IsAlive: true } ||
            found.PlayerID == Caster.PlayerID ||
            found.Controller.Team == Caster.Controller.Team
        )
        {
            base.Use();
            return;
        }

        Target = found;
        _oldPosition = casterPosition.Value;

        if (!Freeze(found))
        {
            Target = null;
            base.Use();
            return;
        }

        _beam = CreateCatchingBeam(casterPawn);
        _beam.EndPos = trace.EndPos;
        _beam.EndPosUpdated();

        _thinker = CreateCatchingThinker();

        base.Use();
    }

    protected override bool CanUse()
    {
        return Caster is { IsValid: true, IsAlive: true } && config.MaxDistance > 0f;
    }

    public override void UnHook()
    {
        CancelCatching();
        base.UnHook();
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

    private CBeam CreateCatchingBeam(CCSPlayerPawn casterPawn)
    {
        var beam = core.EntitySystem.CreateEntity<CBeam>();
        beam.Width = config.BeamWidth;
        beam.Render = new Color(
            config.RedColorEffect,
            config.GreenColorEffect,
            config.BlueColorEffect
        );

        beam.Teleport(casterPawn.EyePosition, casterPawn.AbsRotation, null);
        beam.DispatchSpawn();
        return beam;
    }

    private CGameTrace LaunchTraceFromCaster(CCSPlayerPawn casterPawn, Vector start)
    {
        var direction = MathAlgorithm.ForwardFromAngles(casterPawn.EyeAngles);
        var end = start + direction * config.MaxDistance;

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

        return trace;
    }

    private CancellationTokenSource CreateCatchingThinker()
    {
        return core.Scheduler.RepeatBySeconds(ThinkerInterval, () =>
        {
            var casterPawn = Caster.PlayerPawn;
            var casterPosition = casterPawn?.AbsOrigin;

            if (
                !Caster.IsValid ||
                !Caster.IsAlive ||
                casterPawn is not { IsValid: true } ||
                casterPosition is null ||
                _oldPosition is not { } oldPosition ||
                HasMoved(oldPosition, casterPosition.Value)
            )
            {
                CancelCatching();
                return;
            }

            if (
                Target is not { IsValid: true, IsAlive: true } target ||
                target.Controller.Team == Caster.Controller.Team
            )
            {
                CancelCatching();
                return;
            }

            if ((casterPawn.MovementServices?.Buttons.ButtonPressed & GameButtonFlags.E) == 0)
            {
                CancelCatching();
                return;
            }

            if (!CatchTargetPawn(target))
            {
                CancelCatching();
                return;
            }

            RefreshCatchingBeam(target);
        });
    }

    private bool CatchTargetPawn(IPlayer target)
    {
        var casterPosition = Caster.PlayerPawn?.AbsOrigin;
        var targetPawn = target.PlayerPawn;
        var targetPosition = targetPawn?.AbsOrigin;

        if (casterPosition is null || targetPawn is not { IsValid: true } || targetPosition is null)
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

    private void RefreshCatchingBeam(IPlayer target)
    {
        var targetPosition = target.PlayerPawn?.AbsOrigin;
        if (targetPosition is null)
        {
            return;
        }

        if (_beam is not null)
        {
            _beam.EndPos = targetPosition.Value + new Vector(0f, 0f, BodyPositionZ);
            _beam.EndPosUpdated();
        }
    }

    private bool Freeze(IPlayer player)
    {
        if (player.PlayerPawn is not { IsValid: true } pawn)
        {
            return false;
        }

        _targetMoveType = pawn.MoveType;
        _targetActualMoveType = pawn.ActualMoveType;

        pawn.MoveType = MoveType_t.MOVETYPE_FLYGRAVITY;
        pawn.ActualMoveType = MoveType_t.MOVETYPE_FLYGRAVITY;
        pawn.MoveTypeUpdated();
        pawn.AbsVelocity = Vector.Zero;
        return true;
    }

    private void CancelCatching()
    {
        _thinker?.Cancel();
        _thinker = null;

        if (_beam is { IsValidEntity: true })
        {
            _beam.Despawn();
        }

        _beam = null;

        if (Target is not null)
        {
            UnFreeze(Target);
        }

        Target = null;
        _oldPosition = null;
    }

    private void UnFreeze(IPlayer player)
    {
        if (player.PlayerPawn is { IsValid: true } pawn)
        {
            pawn.MoveType = _targetMoveType ?? MoveType_t.MOVETYPE_WALK;
            pawn.ActualMoveType = _targetActualMoveType ?? MoveType_t.MOVETYPE_WALK;
            pawn.MoveTypeUpdated();
        }

        _targetMoveType = null;
        _targetActualMoveType = null;
    }

    private static bool HasMoved(Vector oldPosition, Vector currentPosition)
    {
        return
            Math.Abs(oldPosition.X - currentPosition.X) > MovementTolerance ||
            Math.Abs(oldPosition.Y - currentPosition.Y) > MovementTolerance ||
            Math.Abs(oldPosition.Z - currentPosition.Z) > MovementTolerance;
    }
}