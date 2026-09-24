using CustomEquipment.Api.Data;
using CustomEquipment.Utils;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Trace;

namespace CustomEquipment.Data.Equipments.Weapons.Equipments.Entities;

/// <summary>
/// Представляет установленную лазерную мину и обработку её луча.
/// </summary>
public sealed class LaserMineEntity : LaserMineEntityBase
{
    private readonly ISwiftlyCore _core;
    private readonly LaserMineSettings _settings;
    private readonly LaserMineSoundPlayback _sounds;

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
        _sounds = new LaserMineSoundPlayback(core, settings, () => LaserMine);
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

    private const string DamageParticle = "particles/explosions_fx/bumpmine_detonate_sparks.vpcf";
    private const DamageTypes_t DamageType = DamageTypes_t.DMG_POISON;

    protected override void OnSpawned()
    {
        if (LaserMine?.AbsOrigin is { } position) _sounds.Start(position, ScheduleWhileAlive);
    }

    protected override void OnDestroyedByDamage()
    {
        if (LaserMine?.AbsOrigin is { } position) _sounds.Destroy(position);
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

        // CreateDamageParticle(hitPoint);
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

        // Не сохраняем временную prop_dynamic как inflictor в native damage bookkeeping.
        // При заражении владельца мина удаляется, а Source 2 может продолжать держать
        // damage handles до конца текущего frame/round reset.
        targetPawn.TakeDamage(
            _settings.DamagePerTrigger,
            DamageType,
            ownerPawn,
            ownerPawn
        );

        if (IsArmed && LaserMine is { IsValidEntity: true, AbsOrigin: { } position })
        {
            _sounds.Damage(position);
        }
    }

    private void UpdateTracer(Vector hitPoint)
    {
        LaserMineTracer?.EndPos = hitPoint == default ? LaserDirection : hitPoint;
        LaserMineTracer?.EndPosUpdated();
    }

    private void CreateDamageParticle(Vector hitPoint)
    {
        var particle = _core.EntitySystem.CreateEntity<CParticleSystem>();

        particle.EffectName = DamageParticle;
        particle.StartActive = true;
        particle.DispatchSpawn();

        particle.Teleport(hitPoint, null, null);
    }
}
