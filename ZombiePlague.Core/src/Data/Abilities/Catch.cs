using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal class Catch(ISwiftlyCore core, CatchConfig config) : BaseActiveAbility(core)
{
    public override KeyKind? Key => KeyKind.E;
    public override float Cooldown => config.CooldownTime;
    
    private CBeam? _beam;
    private CancellationTokenSource? _thinker;
    private Vector? _oldPosition;

    private const float BodyPositionZ = 48f;
    private const float DelayByRepeat = 0.1f;
    
    public override void Use()
    {
        var casterPawn = Caster.PlayerPawn;
        if (casterPawn == null)
        {
            return;
        }
        
        if (Target != null)
        {
            return;
        }

        CancelCatching();
        _oldPosition = casterPawn.AbsOrigin;
        _beam = CreateCatchingBeam(casterPawn);

        var trace = LaunchTraceFromCaster(casterPawn);
        var entity = trace.Entity;
        var found = entity.Address.FindPlayerByPawnAddress(core);

        if (found is null || !found.IsValid || !found.Controller.PawnIsAlive)
        {
            Target = null;
        }
        else
        {
            Target = found;
            Freeze(Target);
        }

        _beam.EndPos = trace.EndPos;
        _thinker = CreateCatchingThinker();

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

    public override void UnHook()
    {
        CancelCatching();
        base.UnHook();
    }

    private CBeam CreateCatchingBeam(CCSPlayerPawn casterPawn)
    {
        var beam = core.EntitySystem.CreateEntity<CBeam>();
        beam.Width = config.BeamWidth;
        beam.Render = new Color(config.RedColorEffect, config.GreenColorEffect, config.BlueColorEffect);

        var playerEyesPosition = casterPawn.EyePosition;
        var playerRotation = casterPawn.AbsRotation;
        beam.Teleport(playerEyesPosition, playerRotation, null);

        beam.DispatchSpawn();
        return beam;
    }

    private CGameTrace LaunchTraceFromCaster(CCSPlayerPawn casterPawn)
    {
        var start = casterPawn.EyePosition!.Value;
        var forward = casterPawn.EyeAngles;

        var trace = new CGameTrace();
        core.Trace.SimpleTrace(
            start,
            forward,
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
        CancellationTokenSource? token = null!;
        token = core.Scheduler.RepeatBySeconds(DelayByRepeat, () =>
        {
            var playerPawn = Caster.PlayerPawn;
            var playerPosition = playerPawn?.AbsOrigin;
            if (playerPawn == null || !playerPawn.IsValid || _oldPosition!.Value != playerPosition!.Value ||
                !Caster.IsOnZombieTeam())
            {
                CancelCatching();
                return;
            }

            if (Target == null || !Target.IsValid || Target.IsOnZombieTeam())
            {
                CancelCatching();
                return;
            }

            if ((playerPawn.MovementServices?.Buttons.ButtonPressed & GameButtonFlags.E) == 0)
            {
                CancelCatching();
                return;
            }

            CatchTargetPawn();
            RefreshCatchingBeam();
        });

        return token;
    }

    private void CatchTargetPawn()
    {
        var casterPawn = Caster.PlayerPawn;
        var targetPawn = Target!.PlayerPawn;

        var targetPosition = targetPawn!.AbsOrigin!;

        var direction = (casterPawn!.AbsOrigin! - targetPosition).Value.Normalized();
        targetPawn.AbsVelocity = direction * config.Strength;
    }

    private void RefreshCatchingBeam()
    {
        var targetPawn = Target?.PlayerPawn;
        if (targetPawn == null)
        {
            return;
        }

        var targetPosition = targetPawn.AbsOrigin;
        if (targetPosition == null)
        {
            return;
        }

        _beam?.EndPos = targetPosition.Value + new Vector(0f, 0f, BodyPositionZ);
        _beam?.EndPosUpdated();
    }

    private void CancelCatching()
    {
        if (_beam != null && _beam.IsValidEntity)
        {
            _beam.Despawn();
        }

        if (Target != null)
        {
            UnFreeze(Target);
        }

        _beam = null;
        _thinker?.Cancel();
        _thinker = null;
        Target = null;
    }

    private void Freeze(IPlayer player)
    {
        var playerPawn = player.PlayerPawn;
        if (playerPawn == null)
        {
            return;
        }
        
        playerPawn.MoveType = MoveType_t.MOVETYPE_FLYGRAVITY;
        playerPawn.ActualMoveType = MoveType_t.MOVETYPE_FLYGRAVITY;
        playerPawn.MoveTypeUpdated();
        
        playerPawn.AbsVelocity = new Vector(0, 0, 0);
    }

    private void UnFreeze(IPlayer player)
    {
        var playerPawn = player.PlayerPawn;
        if (playerPawn == null)
        {
            return;
        }
        
        playerPawn.MoveType = MoveType_t.MOVETYPE_WALK;
        playerPawn.ActualMoveType = MoveType_t.MOVETYPE_WALK;
        playerPawn.MoveTypeUpdated();
    }
}
