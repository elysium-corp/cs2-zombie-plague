using System.ComponentModel;
using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Di;
using CS2ZombiePlague.Service.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Service;

public sealed class WeaponParticleService : IWeaponParticleService
{
    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();

    private sealed class PlayerParticleState
    {
        public Queue<CParticleSystem> Queue { get; } = new();
        public CancellationTokenSource? CleanupToken { get; set; }
    }

    private readonly Dictionary<IPlayer, PlayerParticleState> _stateByPlayer = new();

    private readonly Vector _adaptiveEyeVector = new(0f, 0f, 10f);

    private const float StepBetweenParticle = 50f;
    private const int MaxParticlesPerShot = 25;
    private const int ParticleLifetimeTicks = 2;

    public WeaponParticleService()
    {
        _core.Event.OnClientDisconnected += OnClientDisconnected;
    }

    public void Dispose()
    {
        _core.Event.OnClientDisconnected -= OnClientDisconnected;
    }

    public void OnWeaponFireParticle(IPlayer player, string particleName, WeaponFireParticleType particleType, Vector? impactPos)
    {
        switch (particleType)
        {
            case WeaponFireParticleType.Single:
                SpawnSingle(player, particleName, impactPos);
                break;

            case WeaponFireParticleType.Trail:
                SpawnTrail(player, particleName, impactPos);
                break;

            default:
                throw new InvalidEnumArgumentException("ParticleService.OnWeaponFireParticle: unknown particle type");
        }
    }
    
    public void OnClientDisconnected(IOnClientDisconnectedEvent @event)
    {
        var player = _core.PlayerManager.GetPlayer(@event.PlayerId);

        if (player == null)
        {
            return;
        }

        if (!_stateByPlayer.TryGetValue(player, out var state))
        {
            return;
        }

        state.CleanupToken?.Cancel();
        state.CleanupToken = null;

        while (state.Queue.Count > 0)
        {
            var p = state.Queue.Dequeue();
            if (p.IsValidEntity)
            {
                p.Despawn();
            }
        }

        _stateByPlayer.Remove(player);
    }

    private void SpawnSingle(IPlayer player, string particleName, Vector? impactPos)
    {
        var pawn = player.RequiredPlayerPawn;
        var eyePos = pawn.EyePosition;

        if (eyePos == null)
        {
            return;
        }
        
        var trace = TraceFromPos(pawn, eyePos.Value, impactPos);

        var direction = trace.Direction.Normalized();
        var rotation = direction.ToQAngles();
        var position = trace.StartPos;

        var particle = CreateAndSpawnParticle(particleName, position, rotation);

        EnqueueAndScheduleCleanup(player, particle);
    }

    private void SpawnTrail(IPlayer player, string particleName, Vector? impactPos)
    {
        var pawn = player.RequiredPlayerPawn;
        var eyePos = pawn.EyePosition;

        if (eyePos == null)
        {
            return;
        }
        
        var trace = TraceFromPos(pawn, eyePos.Value, impactPos);

        var direction = trace.Direction.Normalized();
        var rotation = direction.ToQAngles();
        var position = trace.StartPos;

        var byDistance = (int)(trace.Distance / StepBetweenParticle) + 1;
        var count = byDistance > MaxParticlesPerShot ? MaxParticlesPerShot : byDistance;

        var state = GetOrCreateState(player);

        for (var i = 0; i < count; i++)
        {
            var particle = CreateAndSpawnParticle(particleName, position, rotation);
            state.Queue.Enqueue(particle);

            position += direction * StepBetweenParticle;
        }

        ScheduleCleanup(player, state);
    }

    private CParticleSystem CreateAndSpawnParticle(string particleName, Vector position, QAngle rotation)
    {
        var particle = _core.EntitySystem.CreateEntity<CParticleSystem>();
        particle.StartActive = true;
        particle.EffectName = particleName;
        particle.Teleport(position, rotation, null);
        particle.DispatchSpawn();
        return particle;
    }

    private void EnqueueAndScheduleCleanup(IPlayer player, CParticleSystem particle)
    {
        var state = GetOrCreateState(player);
        state.Queue.Enqueue(particle);
        ScheduleCleanup(player, state);
    }

    private PlayerParticleState GetOrCreateState(IPlayer player)
    {
        if (_stateByPlayer.TryGetValue(player, out var state))
        {
            return state;
        }

        state = new PlayerParticleState();
        _stateByPlayer[player] = state;
        return state;
    }

    private void ScheduleCleanup(IPlayer player, PlayerParticleState state)
    {
        if (state.CleanupToken != null)
        {
            state.CleanupToken.Cancel();
            state.CleanupToken = null;
        }

        state.CleanupToken = _core.Scheduler.DelayAndRepeat(
            ParticleLifetimeTicks,
            ParticleLifetimeTicks,
            () =>
            {
                if (!_stateByPlayer.TryGetValue(player, out var s))
                {
                    state.CleanupToken?.Cancel();
                    return;
                }

                if (s.Queue.Count == 0)
                {
                    s.CleanupToken?.Cancel();
                    s.CleanupToken = null;
                    _stateByPlayer.Remove(player);
                    return;
                }

                var p = s.Queue.Dequeue();

                if (p.IsValidEntity)
                {
                    p.Despawn();
                    return;
                }
                
                s.CleanupToken?.Cancel();
                s.CleanupToken = null;

                _stateByPlayer.Remove(player);
            }
        );
    }

    private CGameTrace TraceFromPos(CCSPlayerPawn pawn, Vector eyePos, Vector? impactPos)
    {
        var trace = new CGameTrace();

        if (impactPos != null)
        {
            _core.Trace.SimpleTrace(
                start: eyePos + _adaptiveEyeVector,
                end: impactPos.Value,
                rayKind: RayType_t.RAY_TYPE_LINE,
                objectQuery: RnQueryObjectSet.All,
                interactWith: MaskTrace.Solid |
                              MaskTrace.WorldGeometry |
                              MaskTrace.StaticLevel |
                              MaskTrace.Player |
                              MaskTrace.Npc |
                              MaskTrace.PhysicsProp |
                              MaskTrace.Window |
                              MaskTrace.Debris |
                              MaskTrace.Hitbox,
                interactExclude: MaskTrace.Trigger | MaskTrace.Sky,
                interactAs: MaskTrace.Empty,
                collision: CollisionGroup.Always,
                trace: ref trace,
                filterEntity: pawn,
                filterSecondEntity: null
            );
        }
        else
        {
            var angle = impactPos != null ? (impactPos.Value - eyePos).Normalized().ToQAngles() : pawn.EyeAngles;
            _core.Trace.SimpleTrace(
                start: eyePos + _adaptiveEyeVector,
                angle: angle,
                rayKind: RayType_t.RAY_TYPE_LINE,
                objectQuery: RnQueryObjectSet.All,
                interactWith: MaskTrace.Solid |
                              MaskTrace.WorldGeometry |
                              MaskTrace.StaticLevel |
                              MaskTrace.Player |
                              MaskTrace.Npc |
                              MaskTrace.PhysicsProp |
                              MaskTrace.Window |
                              MaskTrace.Debris |
                              MaskTrace.Hitbox,
                interactExclude: MaskTrace.Trigger | MaskTrace.Sky,
                interactAs: MaskTrace.Empty,
                collision: CollisionGroup.Always,
                trace: ref trace,
                filterEntity: pawn,
                filterSecondEntity: null
            );
        }

        return trace;
    }
}