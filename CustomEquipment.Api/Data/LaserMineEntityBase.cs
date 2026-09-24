using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Microsoft.Extensions.Logging;

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
    private CancellationTokenSource? _healthTask;
    private readonly List<CancellationTokenSource> _scheduledTasks = [];
    private bool _destroyedByDamage;
    private int _disposed;

    /// <summary>Задержка между размещением мины и включением луча, в секундах.</summary>
    public virtual float ArmingDelay => 0f;

    /// <summary>Показывает, завершена ли зарядка мины.</summary>
    public bool IsArmed { get; private set; }

    /// <summary>Последняя подтверждённая позиция установленной мины.</summary>
    protected Vector? LastKnownPosition { get; private set; }

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
            owner.PlayerPawn is not { IsValid: true } playerPawn)
        {
            return false;
        }

        if (!LaserMinePlacement.TryFindSurface(
                core,
                playerPawn,
                maxDistanceToAttach,
                out var position,
                out var rotation))
        {
            return false;
        }

        Owner = owner;

        var team = playerPawn.Team;
        var ownerHandle = core.EntitySystem.GetRefEHandle(playerPawn).Raw;

        try
        {
            LaserMine = core.EntitySystem
                .CreateEntityByDesignerName<CBaseModelEntity>("prop_dynamic_override");

            if (LaserMine is not { IsValidEntity: true })
            {
                Dispose();
                return false;
            }

            LaserMine.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            LaserMine.Collision.CollisionGroup = (byte)CollisionGroup.Debris;

            LaserMine.DispatchSpawn();

            core.Scheduler.NextWorldUpdate(() =>
            {
                if (Volatile.Read(ref _disposed) != 0 ||
                    LaserMine is not { IsValidEntity: true } mine)
                {
                    return;
                }

                mine.SetModel(LaserMineModel);

                mine.Teleport(position, rotation, null);

                LastKnownPosition = position;

                mine.Team = team;

                mine.OwnerEntity.Raw = ownerHandle;
                mine.OwnerEntityUpdated();

                mine.MaxHealth = MaxHealth;
                mine.MaxHealthUpdated();

                mine.Health = MaxHealth;
                mine.HealthUpdated();

                mine.TakesDamage = true;
                mine.TakesDamageUpdated();

                mine.TakeDamageFlags = TakeDamageFlags_t.DFLAG_NONE;
                mine.TakeDamageFlagsUpdated();

                mine.Collision.CollisionGroup = (byte)CollisionGroup.Debris;
                mine.Collision.CollisionAttribute.CollisionGroup =
                    (byte)CollisionGroup.Debris;

                mine.Collision.CollisionGroupUpdated();
                mine.Collision.CollisionAttribute.CollisionGroupUpdated();
                mine.CollisionRulesChanged();

                OnSpawned();

                StartHealthHandler();

                if (ArmingDelay > 0f)
                {
                    _armingTask = core.Scheduler.DelayBySeconds(
                        ArmingDelay,
                        Arm
                    );

                    core.Scheduler.StopOnMapChange(_armingTask);
                }
                else
                {
                    Arm();
                }
            });

            return true;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Обрабатывает успешное размещение перед началом зарядки.</summary>
    protected virtual void OnSpawned()
    {
    }

    /// <summary>Обрабатывает готовность мины после зарядки.</summary>
    protected virtual void OnArmed()
    {
    }

    /// <summary>Обрабатывает уничтожение уроном, до удаления сущности.</summary>
    protected virtual void OnDestroyedByDamage()
    {
    }

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
        CancelTimer(Interlocked.Exchange(ref _healthTask, null));
        foreach (var timer in _scheduledTasks) CancelTimer(timer);
        _scheduledTasks.Clear();
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

    /// <summary>Планирует действие установленной мины; удаление мины отменяет его.</summary>
    protected void ScheduleWhileAlive(float delay, Action action)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        var timer = core.Scheduler.DelayBySeconds(delay, () =>
        {
            if (Volatile.Read(ref _disposed) == 0 && LaserMine is { IsValidEntity: true }) action();
        });
        _scheduledTasks.Add(timer);
        core.Scheduler.StopOnMapChange(timer);
    }

    private void StartTriggerHandler()
    {
        _triggerTask = core.Scheduler.RepeatBySeconds(Math.Max(0.05f, TriggerInterval), () =>
        {
            if (Volatile.Read(ref _disposed) == 0 && IsArmed) Trigger();
        });
        core.Scheduler.StopOnMapChange(_triggerTask);
    }

    private void StartHealthHandler()
    {
        _healthTask = core.Scheduler.RepeatBySeconds(0.05f, () =>
        {
            if (Volatile.Read(ref _disposed) != 0) return;

            var mine = LaserMine;
            if (mine is not { IsValidEntity: true } || mine.Health <= 0)
            {
                // Проверка идёт уже на world update, вне native TakeDamage callback.
                // Это даёт движку применить реальный урон к Health и исключает удаление
                // prop_dynamic непосредственно внутри damage pipeline.
                DestroyByDamage();
            }
        });
        core.Scheduler.StopOnMapChange(_healthTask);
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