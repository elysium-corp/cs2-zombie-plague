using CustomEquipment.Api.Data;
using CustomEquipment.Utils;
using CustomEquipment.Data.GameplayItems;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.Trace;

namespace CustomEquipment.Data.Equipments.Weapons.Equipments.Entities;

/// <summary>
/// Представляет установленную лазерную мину и обработку её луча.
/// </summary>
public sealed class LaserMineEntity : LaserMineEntityBase
{
    private readonly ISwiftlyCore _core;
    private readonly LaserMineSettings _settings;
    private long? _lastDamageSoundAt;

    /// <summary>
    /// Создаёт сущность с параметрами лазерной мины по умолчанию.
    /// </summary>
    /// <param name="core">Ядро SwiftlyS2.</param>
    public LaserMineEntity(ISwiftlyCore core)
        : this(
            core,
            (LaserMineSettings)GameplayItemDefaults.Get(GameplayItemKeys.LaserMine).Settings
        )
    {
    }

    internal LaserMineEntity(ISwiftlyCore core, LaserMineSettings settings)
        : base(core)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public override string LaserMineModel => _settings.MineModel;
    public override float TriggerInterval => _settings.TriggerInterval;
    public override float TracerDistance => _settings.TracerDistance;
    public override int MaxHealth => _settings.MaxHealth;
    public override float ArmingDelay => _settings.ActivationDelay;
    public override float BeamWidth => _settings.BeamWidth;

    public override Color BeamColor => new(
        _settings.BeamRed,
        _settings.BeamGreen,
        _settings.BeamBlue,
        _settings.BeamAlpha
    );

    private const DamageTypes_t DamageType = DamageTypes_t.DMG_POISON;

    protected override void OnSpawned()
    {
        if (LaserMine?.AbsOrigin is not { } position) return;

        PlaySound(_settings.InstallSound, position);
        ScheduleWhileAlive(_settings.InstallSoundDuration, () =>
        {
            if (LaserMine?.AbsOrigin is { } origin) PlaySound(_settings.ChargeSound, origin);
        });
        ScheduleWhileAlive(_settings.ReadySoundDelay, () =>
        {
            if (LaserMine?.AbsOrigin is { } origin) PlaySound(_settings.ReadySound, origin);
        });
    }

    protected override void OnDestroyedByDamage()
    {
        var position = LaserMine?.AbsOrigin ?? LastKnownPosition;
        if (position is not { } origin) return;

        var guid = PlaySound(_settings.DestroySound, origin);
        StopSoundLater(guid, _settings.DestroySoundDuration);
    }

    protected override void Trigger()
    {
        if (LaserMine == null || LaserMineTracer == null || !LaserMine.IsValidEntity || !LaserMineTracer.IsValidEntity)
        {
            Destroy();
            return;
        }

        var owner = Owner;
        if (owner is not { IsValid: true })
        {
            Destroy();
            return;
        }

        var foundTarget = TryFindTarget(out var target, out var hitPoint);

        UpdateTracer(hitPoint);

        if (!foundTarget) return;

        ApplyDamage(target, owner);
    }

    private bool TryFindTarget(out IPlayer target, out Vector hitPoint)
    {
        target = null!;
        hitPoint = default;

        if (LaserMine!.AbsRotation == null) return false;

        var forward = ForwardFromAngles(LaserMine.AbsRotation.Value);
        var start = LaserMine.AbsOrigin;

        if (start == null) return false;

        var end = start + forward * TracerDistance;

        if (end == null) return false;

        var trace = _core.Trace.TraceShapeLine(
            start.Value,
            end.Value,
            new TraceParams
            {
                ObjectQuery = RnQueryObjectSet.AllGameEntities | RnQueryObjectSet.Static,
                InteractWith = MaskTrace.Solid | MaskTrace.Player,
                InteractExclude = MaskTrace.Empty,
                InteractAs = MaskTrace.Empty,
                EntitiesToIgnore = [LaserMine]
            }
        );

        hitPoint = trace.EndPos;

        var entity = trace.Entity;
        if (entity is null) return false;

        var found = entity.Address.FindPlayerByPawnAddress();

        if (found is null || !found.IsValid || !found.IsAlive) return false;

        target = found;
        return true;
    }

    private void ApplyDamage(IPlayer target, IPlayer owner)
    {
        var targetPawn = target.PlayerPawn;
        var ownerPawn = owner.PlayerPawn;
        var mine = LaserMine;

        if (targetPawn is not { IsValid: true } ||
            ownerPawn is not { IsValid: true } ||
            mine is not { IsValidEntity: true } ||
            targetPawn.Team == mine.Team || _settings.DamagePerTrigger <= 0f)
        {
            return;
        }

        targetPawn.TakeDamage(
            _settings.DamagePerTrigger,
            DamageType
        );

        if (IsArmed && LaserMine is { IsValidEntity: true, AbsOrigin: { } position })
        {
            PlayDamageSound(position);
        }
    }

    private void PlayDamageSound(Vector position)
    {
        var now = Environment.TickCount64;
        var interval = (long)MathF.Ceiling(_settings.DamageSoundInterval * 1000f);

        if (_lastDamageSoundAt is { } last && now - last < interval)
        {
            return;
        }

        _lastDamageSoundAt = now;
        PlaySound(_settings.DamageSound, position);
    }

    private uint PlaySound(string name, Vector position)
    {
        if (string.IsNullOrWhiteSpace(name) || _settings.SoundVolume <= 0f)
        {
            return 0;
        }

        try
        {
            return SoundExt.PlayInPlace(name, position, _settings.SoundVolume);
        }
        catch (Exception exception)
        {
            _core.Logger.LogWarning(exception, "[LaserMine] Не удалось воспроизвести звук {Sound}.", name);
            return 0;
        }
    }

    private void StopSoundLater(uint guid, float duration)
    {
        if (guid == 0 || duration <= 0f) return;

        var timer = _core.Scheduler.DelayBySeconds(duration, () =>
        {
            try
            {
                _core.NetMessage.Send<CMsgSosStopSoundEvent>(message =>
                {
                    message.SoundeventGuid = unchecked((int)guid);
                    message.Recipients.AddAllPlayers();
                });
            }
            catch (Exception exception)
            {
                _core.Logger.LogWarning(exception, "[LaserMine] Не удалось остановить звук взрыва.");
            }
        });

        _core.Scheduler.StopOnMapChange(timer);
    }

    private void UpdateTracer(Vector hitPoint)
    {
        LaserMineTracer?.EndPos = hitPoint == default ? LaserDirection : hitPoint;
        LaserMineTracer?.EndPosUpdated();
    }
}