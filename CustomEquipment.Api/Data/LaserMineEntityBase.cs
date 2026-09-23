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
    private CancellationTokenSource? _armingTask;
    private bool _destroyedByDamage;
    private int _disposed;

    /// <summary>Задержка между размещением мины и включением луча, в секундах.</summary>
    public virtual float ArmingDelay => 0f;

    /// <summary>Показывает, завершена ли зарядка мины.</summary>
    public bool IsArmed { get; private set; }

    /// <summary>Размещает мину на поверхности перед владельцем.</summary>
    public void Spawn(IPlayer owner) => TrySpawn(owner);

    /// <summary>
    /// Проверяет поверхность до создания сущностей и возвращает результат размещения.
    /// Вызывается только в игровом потоке; при отказе предмет владельца не списывается.
    /// </summary>
    public bool TrySpawn(IPlayer owner, float maxDistanceToAttach = 10000f)
    {
        if (Volatile.Read(ref _disposed) != 0 || LaserMine != null) return false;
        if (owner is not { IsValid: true, IsAlive: true } ||
            owner.PlayerPawn is not { IsValid: true } playerPawn) return false;

        // Новая модель ещё не участвует в трассировке и не может закрыть поверхность.
        if (!TryGetPlacement(playerPawn, maxDistanceToAttach, out var position, out var rotation)) return false;

        Owner = owner;
        var team = playerPawn.Team;

        try
        {
            LaserMine = core.EntitySystem.CreateEntityByDesignerName<CBaseModelEntity>("prop_dynamic_override");
            if (LaserMine is not { IsValidEntity: true })
            {
                Dispose();
                return false;
            }

            LaserMine.SetModel(LaserMineModel);
            LaserMine.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            LaserMine.Collision.CollisionGroup = (byte)CollisionGroup.Debris;
            LaserMine.DispatchSpawn();
            LaserMine.Teleport(position, rotation, null);

            LaserMine.Team = team;
            LaserMine.MaxHealth = MaxHealth;
            LaserMine.MaxHealthUpdated();
            LaserMine.Health = MaxHealth;
            LaserMine.HealthUpdated();
            LaserMine.TakesDamage = true;
            LaserMine.TakesDamageUpdated();
            LaserMine.TakeDamageFlags = TakeDamageFlags_t.DFLAG_NONE;
            LaserMine.TakeDamageFlagsUpdated();

            // После DispatchSpawn обновляем физику: игроки проходят, попадания сохраняются.
            LaserMine.Collision.CollisionGroup = (byte)CollisionGroup.Debris;
            LaserMine.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.Debris;
            LaserMine.Collision.CollisionGroupUpdated();
            LaserMine.Collision.CollisionAttribute.CollisionGroupUpdated();
            LaserMine.CollisionRulesChanged();

            OnSpawned();
            if (ArmingDelay > 0f)
            {
                _armingTask = core.Scheduler.DelayBySeconds(ArmingDelay, Arm);
                core.Scheduler.StopOnMapChange(_armingTask);
            }
            else
            {
                Arm();
            }

            return LaserMine is { IsValidEntity: true };
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Обрабатывает успешное размещение перед началом зарядки.</summary>
    protected virtual void OnSpawned() { }

    /// <summary>Обрабатывает готовность мины после зарядки.</summary>
    protected virtual void OnArmed() { }

    /// <summary>Обрабатывает уничтожение уроном, до удаления сущности.</summary>
    protected virtual void OnDestroyedByDamage() { }

    /// <summary>Уничтожает мину уроном; обычный Dispose удаляет её без эффекта взрыва.</summary>
    public void DestroyByDamage()
    {
        if (Volatile.Read(ref _disposed) != 0 || _destroyedByDamage) return;
        _destroyedByDamage = true;
        try
        {
            OnDestroyedByDamage();
        }
        finally
        {
            Dispose();
        }
    }

    private void Arm()
    {
        if (Volatile.Read(ref _disposed) != 0 || IsArmed) return;
        if (LaserMine is not { IsValidEntity: true } || Owner is not { IsValid: true })
        {
            Dispose();
            return;
        }

        try
        {
            LaserMineTracer = core.EntitySystem.CreateEntity<CBeam>();
            LaserMineTracer.Width = BeamWidth;
            LaserMineTracer.Render = BeamColor;
            LaserMineTracer.DispatchSpawn();
            LaserMineTracer.Teleport(LaserMine.AbsOrigin, LaserMine.AbsRotation, null);
            ConfigureTracer();
            IsArmed = true;
            OnArmed();
            // Первый вызов RepeatBySeconds не ждёт интервала: команда уже настроена.
            if (TriggerInterval > 0) StartTriggerHandler();
        }
        catch
        {
            Dispose();
            throw;
        }
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
        CancelTimer(Interlocked.Exchange(ref _armingTask, null));
        CancelTimer(Interlocked.Exchange(ref _triggerTask, null));
        IsArmed = false;

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

    private bool TryGetPlacement(
        CCSPlayerPawn playerPawn,
        float maxDistanceToAttach,
        out Vector position,
        out QAngle rotation)
    {
        position = default;
        rotation = default;

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
                EntitiesToIgnore = [playerPawn]
            }
        );

        if (!trace.DidHit || trace.StartInSolid || trace.Distance > maxDistanceToAttach) return false;

        var normal = trace.HitNormal;

        position = trace.EndPos + normal * 5;
        rotation = normal.ToQAngles();

        return true;
    }

    private void StartTriggerHandler()
    {
        _triggerTask = core.Scheduler.RepeatBySeconds(Math.Max(0.05f, TriggerInterval), () =>
        {
            if (Volatile.Read(ref _disposed) == 0 && IsArmed) Trigger();
        });
        core.Scheduler.StopOnMapChange(_triggerTask);
    }

    private static void CancelTimer(CancellationTokenSource? timer)
    {
        try
        {
            timer?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Планировщик мог освободить таймер до удаления мины.
        }
    }
}
