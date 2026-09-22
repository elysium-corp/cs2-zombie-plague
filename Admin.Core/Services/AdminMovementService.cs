using Admin.Api.Permissions;
using Admin.Core.Data;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Trace;

namespace Admin.Core.Services;

internal sealed class AdminMovementService(ISwiftlyCore core, IPrivilegeService privileges) : IDisposable
{
    private sealed record MovementState(PlayerActionTarget Player, uint PawnHandle,
        MoveType_t MoveType, MoveType_t ActualMoveType);
    private sealed record GrabState(MovementState Target, PlayerActionTarget Administrator,
        uint AdministratorPawnHandle, float Distance);

    private readonly Dictionary<ulong, MovementState> _noclip = [];
    private readonly Dictionary<ulong, GrabState> _grabs = [];
    private bool _started;

    public void Start()
    {
        if (_started) return;
        _started = true;
        core.Event.OnTick += OnTick;
        core.Event.OnMapUnload += OnMapUnload;
        core.Event.OnClientDisconnected += OnDisconnect;
    }

    public bool SetNoclip(IPlayer target, bool enabled)
    {
        if (!_started || !TryPawn(target, out var pawn)) return false;
        if (!enabled)
        {
            if (_noclip.Remove(target.SessionId, out var previous)) Restore(previous, MoveType_t.MOVETYPE_NOCLIP);
            return true;
        }

        if (_noclip.ContainsKey(target.SessionId)) return true;
        if (IsGrabbed(target) || pawn.MoveType != MoveType_t.MOVETYPE_WALK) return false;
        _noclip[target.SessionId] = Capture(target, pawn);
        SetMovement(pawn, MoveType_t.MOVETYPE_NOCLIP);
        return true;
    }

    public bool HasNoclip(IPlayer target) => _noclip.ContainsKey(target.SessionId);
    public bool HasGrab(IPlayer administrator) => _grabs.ContainsKey(administrator.SessionId);

    public IPlayer? FindAimTarget(IPlayer administrator)
    {
        if (!TryPawn(administrator, out var pawn) || pawn.EyePosition is not { } eye) return null;
        var trace = core.Trace.TraceShapeAngle(eye, pawn.EyeAngles, 1024f, new TraceParams
        {
            ObjectQuery = RnQueryObjectSet.AllGameEntities | RnQueryObjectSet.Static,
            InteractWith = MaskTrace.Solid | MaskTrace.Player,
            EntitiesToIgnore = [pawn]
        });
        return core.PlayerManager.GetAllValidPlayers()
            .FirstOrDefault(player => player.IsAlive && player.PlayerPawn?.Address == trace.Entity?.Address);
    }

    public bool Grab(IPlayer administrator, IPlayer target)
    {
        if (!_started || administrator.SessionId == target.SessionId ||
            !TryPawn(administrator, out var caster) || !TryPawn(target, out var pawn) ||
            caster.EyePosition is not { } eye || pawn.AbsOrigin is not { } origin ||
            HasNoclip(target) || IsGrabbed(target) || HasGrab(target) || IsGrabbed(administrator) ||
            pawn.MoveType != MoveType_t.MOVETYPE_WALK)
            return false;

        var center = origin + new Vector(0, 0, 36);
        var distance = eye.Distance(center);
        if (distance > 1024f) return false;
        var sight = core.Trace.TraceShapeLine(eye, center, new TraceParams
        {
            ObjectQuery = RnQueryObjectSet.AllGameEntities | RnQueryObjectSet.Static,
            InteractWith = MaskTrace.Solid | MaskTrace.Player,
            EntitiesToIgnore = [caster, pawn]
        });
        if (sight.DidHit) return false;

        Release(administrator);
        _grabs[administrator.SessionId] = new GrabState(Capture(target, pawn),
            PlayerActionTarget.From(administrator), core.EntitySystem.GetRefEHandle(caster).Raw,
            Math.Clamp(distance, 96f, 512f));
        SetMovement(pawn, MoveType_t.MOVETYPE_FLY);
        return true;
    }

    public void Release(IPlayer administrator) => Release(administrator.SessionId);

    private void Release(ulong sessionId)
    {
        if (_grabs.Remove(sessionId, out var state)) Restore(state.Target, MoveType_t.MOVETYPE_FLY);
    }

    private bool IsGrabbed(IPlayer player) =>
        _grabs.Values.Any(state => state.Target.Player.SessionId == player.SessionId);

    private MovementState Capture(IPlayer player, CCSPlayerPawn pawn) =>
        new(PlayerActionTarget.From(player), core.EntitySystem.GetRefEHandle(pawn).Raw,
            pawn.MoveType, pawn.ActualMoveType);

    private static bool TryPawn(IPlayer? player, out CCSPlayerPawn pawn)
    {
        pawn = null!;
        if (player is not { IsValid: true, IsAlive: true } || player.PlayerPawn is not { IsValid: true } current)
            return false;
        pawn = current;
        return true;
    }

    private CCSPlayerPawn? Resolve(MovementState state, bool requireAlive = true)
    {
        var player = state.Player.Resolve(core);
        if (player is null || requireAlive && !player.IsAlive ||
            player.PlayerPawn is not { IsValid: true } pawn) return null;
        return core.EntitySystem.GetRefEHandle(pawn).Raw == state.PawnHandle ? pawn : null;
    }

    private static void SetMovement(CCSPlayerPawn pawn, MoveType_t movement)
    {
        pawn.MoveType = movement;
        pawn.ActualMoveType = movement;
        pawn.MoveTypeUpdated();
        pawn.AbsVelocity = Vector.Zero;
    }

    private void Restore(MovementState state, MoveType_t expected)
    {
        if (Resolve(state, requireAlive: false) is not { } pawn || pawn.MoveType != expected || pawn.ActualMoveType != expected) return;
        pawn.MoveType = state.MoveType;
        pawn.ActualMoveType = state.ActualMoveType;
        pawn.MoveTypeUpdated();
        pawn.AbsVelocity = Vector.Zero;
    }

    private void OnTick()
    {
        foreach (var (session, state) in _noclip.ToArray())
        {
            if (Resolve(state) is { MoveType: MoveType_t.MOVETYPE_NOCLIP }) continue;
            Restore(state, MoveType_t.MOVETYPE_NOCLIP);
            _noclip.Remove(session);
        }

        foreach (var (session, grab) in _grabs.ToArray())
        {
            var administrator = grab.Administrator.Resolve(core);
            var pawn = Resolve(grab.Target);
            if (!TryPawn(administrator, out var caster) || pawn is not { MoveType: MoveType_t.MOVETYPE_FLY } ||
                core.EntitySystem.GetRefEHandle(caster).Raw != grab.AdministratorPawnHandle ||
                !privileges.HasPermission(administrator!.SteamID, AdminPermissions.Grab) ||
                caster.EyePosition is not { } eye || pawn.AbsOrigin is not { } origin)
            {
                Release(session);
                continue;
            }

            // Точка удержания следует за прицелом, а трассировка ограничивает её препятствиями.
            var pitch = caster.EyeAngles.Pitch * MathF.PI / 180f;
            var yaw = caster.EyeAngles.Yaw * MathF.PI / 180f;
            var forward = new Vector(MathF.Cos(pitch) * MathF.Cos(yaw), MathF.Cos(pitch) * MathF.Sin(yaw), -MathF.Sin(pitch));
            var destination = eye + forward * grab.Distance;
            var trace = core.Trace.TraceShapeLine(eye, destination, new TraceParams
            {
                ObjectQuery = RnQueryObjectSet.AllGameEntities | RnQueryObjectSet.Static,
                InteractWith = MaskTrace.Solid | MaskTrace.Player,
                EntitiesToIgnore = [caster, pawn]
            });
            if (trace.DidHit) destination = trace.EndPos - forward * 32f;
            var destinationOrigin = destination - new Vector(0, 0, 36);
            var sweep = core.Trace.TracePlayerBBox(origin, destinationOrigin,
                new BBox_t { Mins = new Vector(-16, -16, 0), Maxs = new Vector(16, 16, 72) },
                new TraceParams
                {
                    ObjectQuery = RnQueryObjectSet.AllGameEntities | RnQueryObjectSet.Static,
                    InteractWith = MaskTrace.Solid | MaskTrace.Player,
                    EntitiesToIgnore = [pawn]
                });
            if (sweep.DidHit) destinationOrigin = sweep.EndPos;
            var delta = destinationOrigin - origin;
            var distance = delta.Length();
            if (distance > 1536f || trace.StartInSolid || sweep.StartInSolid)
            {
                Release(session);
                continue;
            }
            // Удерживаем игрока в рассчитанной точке и сбрасываем остаточную скорость.
            pawn.Teleport(destinationOrigin, null, Vector.Zero);
        }
    }

    private void OnDisconnect(IOnClientDisconnectedEvent args)
    {
        foreach (var (session, grab) in _grabs.ToArray())
            if (grab.Administrator.PlayerId == args.PlayerId || grab.Target.Player.PlayerId == args.PlayerId) Release(session);
        foreach (var (session, state) in _noclip.ToArray())
            if (state.Player.PlayerId == args.PlayerId) _noclip.Remove(session);
    }

    private void Reset()
    {
        foreach (var session in _grabs.Keys.ToArray()) Release(session);
        foreach (var state in _noclip.Values) Restore(state, MoveType_t.MOVETYPE_NOCLIP);
        _noclip.Clear();
    }

    private void OnMapUnload(IOnMapUnloadEvent args) => Reset();

    public void Dispose()
    {
        if (!_started) return;
        _started = false;
        core.Event.OnTick -= OnTick;
        core.Event.OnMapUnload -= OnMapUnload;
        core.Event.OnClientDisconnected -= OnDisconnect;
        Reset();
    }
}
