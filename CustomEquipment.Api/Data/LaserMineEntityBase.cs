using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Trace;

namespace CustomEquipment.Api.Data;

public abstract class LaserMineEntityBase(ISwiftlyCore core) : IDisposable
{
    public virtual string LaserMineModel =>
        "models/de_overpass/decorations/security_camera/security_camera_1_base.vmdl";

    public CBaseModelEntity? LaserMine { get; private set; }
    public virtual float TriggerInterval => 0f;
    public virtual float TracerDistance => 2000f;
    public virtual int MaxHealth => 100;

    /// <summary>Возвращает ширину луча лазерной мины.</summary>
    public virtual float BeamWidth => 0.5f;

    /// <summary>Возвращает RGBA-цвет луча лазерной мины.</summary>
    public virtual Color BeamColor => new(0, 0, 255, 255);
    protected CBeam? LaserMineTracer { get; private set; }
    protected Vector LaserDirection { get; private set; }
    protected IPlayer? Owner { get; private set; }
    private CancellationTokenSource? _triggerTask;
    private int _disposed;

    public void Spawn(IPlayer owner)
    {
        if (Volatile.Read(ref _disposed) != 0 || LaserMine != null) return;

        Owner = owner;

        var playerPawn = owner.PlayerPawn;

        if (playerPawn is not { IsValid: true }) return;

        var team = playerPawn.Team;

        LaserMine = core.EntitySystem.CreateEntityByDesignerName<CBaseModelEntity>("prop_dynamic_override");
        LaserMine.SetModel(LaserMineModel);

        LaserMine.Collision.CollisionGroup = (byte)CollisionGroup.Debris;
        LaserMine.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;

        LaserMine.DispatchSpawn();
        LaserMine.Team = team;

        core.Scheduler.NextTick(() =>
        {
            if (Volatile.Read(ref _disposed) != 0 || LaserMine is not { IsValidEntity: true }) return;

            // Мина остаётся доступной для попаданий, но не блокирует движение игроков.
            LaserMine.Collision.CollisionGroup = (byte)CollisionGroup.Debris;
            LaserMine.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.Debris;
            LaserMine.Collision.CollisionGroupUpdated();
            LaserMine.Collision.CollisionAttribute.CollisionGroupUpdated();
            LaserMine.CollisionRulesChanged();

            LaserMine.MaxHealth = MaxHealth;
            LaserMine.MaxHealthUpdated();

            LaserMine.Health = MaxHealth;
            LaserMine.HealthUpdated();

            LaserMine.TakesDamage = true;
            LaserMine.TakesDamageUpdated();

            LaserMine.TakeDamageFlags = TakeDamageFlags_t.DFLAG_NONE;
            LaserMine.TakeDamageFlagsUpdated();

            LaserMine.Team = team;

            // RepeatBySeconds может вызвать первый тик сразу: запускаем луч только после настройки мины.
            if (TriggerInterval > 0)
            {
                StartTriggerHandler();
            }
        });

        LaserMineTracer = core.EntitySystem.CreateEntity<CBeam>();
        LaserMineTracer.Width = BeamWidth;
        LaserMineTracer.Render = BeamColor;
        LaserMineTracer.DispatchSpawn();

        if (!TryAttachToGround())
        {
            Destroy();
            return;
        }

        ConfigureTracer();
    }

    protected virtual void Trigger()
    {
    }

    protected Vector ForwardFromAngles(QAngle angles)
    {
        const float deg2Rad = MathF.PI / 180f;

        var pitch = angles.Pitch * deg2Rad;
        var yaw = angles.Yaw * deg2Rad;

        var cosPitch = MathF.Cos(pitch);

        return new Vector(
            cosPitch * MathF.Cos(yaw),
            cosPitch * MathF.Sin(yaw),
            -MathF.Sin(pitch)
        );
    }

    protected void Destroy() => Dispose();

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        // Сначала останавливаем повторяющийся callback. Иначе он может снова обратиться
        // к native entity во время её удаления или reset раунда.
        var triggerTask = Interlocked.Exchange(ref _triggerTask, null);
        try
        {
            triggerTask?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Scheduler владеет жизненным циклом timer; cleanup сущностей всё равно должен продолжиться.
        }

        Owner = null;

        // Отвязываем managed-ссылки до Despawn, чтобы повторный/reentrant cleanup
        // не мог получить entity, которая уже уходит в staging list Source 2.
        var tracer = LaserMineTracer;
        LaserMineTracer = null;

        var mine = LaserMine;
        LaserMine = null;

        if (tracer?.IsValidEntity == true)
        {
            tracer.Despawn();
        }

        if (mine?.IsValidEntity == true)
        {
            mine.Despawn();
        }
    }

    private void ConfigureTracer()
    {
        if (LaserMine == null || LaserMineTracer == null || !LaserMine.IsValidEntity ||
            !LaserMineTracer.IsValidEntity) return;

        if (LaserMine.AbsRotation == null) return;

        var forward = ForwardFromAngles(LaserMine.AbsRotation.Value);
        var start = LaserMine.AbsOrigin;

        if (start == null) return;

        var end = start + forward * TracerDistance;

        if (end == null) return;

        LaserDirection = end.Value;

        LaserMineTracer.EndPos = LaserDirection;
        LaserMineTracer?.EndPosUpdated();
    }

    private bool TryAttachToGround()
    {
        if (LaserMine?.IsValidEntity != true ||
            LaserMineTracer?.IsValidEntity != true)
            return false;

        if (!TryGetPlacement(out var position, out var rotation))
            return false;

        LaserMine.Teleport(position, rotation, null);
        LaserMineTracer.Teleport(position, LaserMine.AbsRotation, null);

        return true;
    }

    private bool TryGetPlacement(out Vector position, out QAngle rotation)
    {
        position = default!;
        rotation = default!;

        if (Owner == null) return false;

        var playerPawn = Owner.PlayerPawn;
        if (playerPawn == null || !playerPawn.IsValid) return false;

        if (LaserMine == null || !LaserMine.IsValidEntity || LaserMineTracer == null ||
            !LaserMineTracer.IsValidEntity) return false;

        if (playerPawn.EyePosition == null) return false;

        var start = playerPawn.EyePosition.Value;
        var forward = playerPawn.EyeAngles;

        var trace = core.Trace.TraceShapeAngle(
            start,
            forward,
            new TraceParams
            {
                ObjectQuery = RnQueryObjectSet.AllGameEntities | RnQueryObjectSet.Static,
                InteractWith = MaskTrace.Solid,
                InteractExclude = MaskTrace.Empty | MaskTrace.Player,
                InteractAs = MaskTrace.Empty,
                EntitiesToIgnore = [playerPawn, LaserMine, LaserMineTracer]
            }
        );

        if (!trace.DidHit) return false;

        var normal = trace.HitNormal;

        position = trace.EndPos + normal * 5;
        rotation = normal.ToQAngles();

        return true;
    }

    private void StartTriggerHandler()
    {
        _triggerTask = core.Scheduler.RepeatBySeconds(Math.Max(0.05f, TriggerInterval), () =>
        {
            if (Volatile.Read(ref _disposed) == 0) Trigger();
        });
    }
}
