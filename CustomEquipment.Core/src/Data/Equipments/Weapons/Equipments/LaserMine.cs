using Common.Di;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Weapons.Equipments.Contracts;
using CustomEquipment.Utils;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Trace;

namespace CustomEquipment.Data.Equipments.Weapons.Equipments;

public sealed class LaserMine : EquipmentItemBase
{
    public override string InheritorName => WeaponName.LaserMine;
    
    public override string DisplayName => "Laser Mine";
    
    public override string InternalName => "custom_equipment:laser_mine";
    
    public override string SubclassName => "";
    
    public override Slot Slot => Slot.Equipment;
    
    public override string Model => "";
    
    public override WeaponType WeaponType => WeaponType.Equipment;

    private const float SetupDuration = 3.0f;
    private const int UpdateIntervalMs = 100;

    private IMenuAPI? _setupWindow;
    private float _setupProgress;

    public override void OnPurchase(IPlayer player)
    {
        if (player.PlayerPawn?.Team == Team.T) return;

        CreateSetupWindow(player);
    }

    private void CreateSetupWindow(IPlayer player)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();

        _setupProgress = 0f;

        var progressBar = new ProgressBarMenuOption(
            "Установка...",
            () => _setupProgress,
            multiLine: false,
            showPercentage: true,
            filledChar: "█",
            emptyChar: "░",
            updateIntervalMs: UpdateIntervalMs
        );

        _setupWindow = core.MenusAPI.CreateBuilder()
            .DisableExit()
            .DisableSound()
            .SetAutoCloseDelay()
            .Design.SetMenuTitleVisible(false)
            .Design.SetMenuFooterVisible(false)
            .Design.SetMaxVisibleItems(1)
            .AddOption(progressBar)
            .Build();

        core.MenusAPI.OpenMenuForPlayer(player, _setupWindow);

        _ = StartSetupProgress(player);
    }

    private async Task StartSetupProgress(IPlayer player)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();

        var elapsed = 0f;

        while (elapsed < SetupDuration)
        {
            await Task.Delay(UpdateIntervalMs);

            if (!player.IsValid) return;

            elapsed += UpdateIntervalMs / 1000f;

            _setupProgress = Math.Clamp(elapsed / SetupDuration, 0f, 1f);
        }

        await Task.Delay(500);

        if (!player.IsValid) return;

        core.MenusAPI.CloseActiveMenu(player);

        core.Scheduler.NextTick(() => { SpawnMine(player); });
    }

    private void SpawnMine(IPlayer player)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();

        var laserMine = new LaserMineEntity(core);
        laserMine.Spawn(player);
    }
}

public sealed class LaserMineEntity(ISwiftlyCore core) : BaseLaserMineEntity(core)
{
    public override string LaserMineModel => "models/lasermine.vmdl";
    public override float TriggerInterval => 0.15f;

    private const string DamageParticle = "particles/explosions_fx/bumpmine_detonate_sparks.vpcf";
    private const float DamagePerTrigger = 25f;
    private const DamageTypes_t DamageType = DamageTypes_t.DMG_POISON;

    protected override void Trigger()
    {
        if (LaserMine == null || LaserMineTracer == null || !LaserMine.IsValidEntity || !LaserMineTracer.IsValidEntity)
        {
            Destroy();
            return;
        }

        if (Owner == null || Owner.PlayerPawn?.Team == Team.T)
        {
            Destroy();
            return;
        }

        var foundTarget = TryFindTarget(out var target, out var hitPoint);

        UpdateTracer(hitPoint);

        if (!foundTarget)
            return;

        ApplyDamage(target);

        CreateDamageParticle(hitPoint);
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

        var trace = core.Trace.TraceShapeLine(
            start.Value,
            end.Value,
            new TraceParams
            {
                ObjectQuery = RnQueryObjectSet.AllGameEntities | RnQueryObjectSet.Static,
                InteractWith = MaskTrace.Solid | MaskTrace.Player,
                InteractExclude = MaskTrace.Empty,
                InteractAs = MaskTrace.Empty,
            }
        );

        var entity = trace.Entity;
        if (entity is null) return false;

        var found = entity.Address.FindPlayerByPawnAddress();
        if (found is null || !found.IsValid || !found.IsAlive) return false;

        target = found;
        hitPoint = trace.EndPos;
        return true;
    }

    private void ApplyDamage(IPlayer target)
    {
        if (target.PlayerPawn?.Team == Team.T)
            target.PlayerPawn?.TakeDamage(DamagePerTrigger, DamageType, Owner?.PlayerPawn);
    }

    private void UpdateTracer(Vector hitPoint)
    {
        LaserMineTracer?.EndPos = hitPoint == default ? LaserDirection : hitPoint;
        LaserMineTracer?.EndPosUpdated();
    }

    private void CreateDamageParticle(Vector hitPoint)
    {
        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();

        particle.EffectName = DamageParticle;
        particle.StartActive = true;
        particle.DispatchSpawn();

        particle.Teleport(hitPoint, null, null);
    }
}