using System.ComponentModel.DataAnnotations;
using SupplyBox.Data.Configs;

namespace SupplyBox.Configuration;

internal sealed class SupplyBoxMapOverrides
{
    [Range(1, 3600)] public int? FirstDropDelaySeconds { get; set; }
    [Range(1, 3600)] public int? RespawnTimeBySeconds { get; set; }
    [Range(0, 3600)] public int? TimeSpreadBySeconds { get; set; }
    [Range(1, 32)] public int? MaxCountTogether { get; set; }
    [Range(0, 100)] public int? ChanceDrop { get; set; }
    [Range(1, 16)] public int? BoxesPerWave { get; set; }
    [Range(0, 1000)] public int? MaxDropsPerRound { get; set; }
    [Range(0, 10000)] public int? MaxDropsPerMap { get; set; }
    [Range(1, 1000)] public int? StartFromRound { get; set; }
    [Range(1, 1000)] public int? EveryNthRound { get; set; }
    [Range(0, 64)] public int? MinPlayers { get; set; }
    [Range(0, 64)] public int? MinAliveHumans { get; set; }
    [Range(0, 64)] public int? MinAliveZombies { get; set; }
    public bool? CountBots { get; set; }
    public bool? AllowSurvivorRound { get; set; }
    public bool? AllowNemesisRound { get; set; }
    public bool? HumansCanCollect { get; set; }
    public bool? ZombiesCanCollect { get; set; }
    [Range(0, 4096)] public int? DropHeight { get; set; }
    [Range(10, 1000)] public int? FallSpeed { get; set; }
    [Range(16, 256)] public int? PickupRadius { get; set; }
    [Range(0, 3600)] public int? LifetimeSeconds { get; set; }
    [Range(0, 1000)] public int? MaxCollectionsPerPlayerPerRound { get; set; }
    [Range(0, 3600)] public int? PlayerCooldownSeconds { get; set; }
    [Range(1, 100000)] public int? HealthCap { get; set; }
    [Range(1, 1000)] public int? ArmorCap { get; set; }
    public string? ParachuteModel { get; set; }
    [StringLength(128)] public string? ParachuteSound { get; set; }
    public List<string>? DropSoundEvents { get; set; }

    // Отдельный объект не позволяет настройкам карты менять общий снимок.
    public static SupplyBoxConfig Resolve(SupplyBoxConfig global, SupplyBoxMap? map) => new()
    {
        Enabled = global.Enabled,
        FirstDropDelaySeconds = map?.Overrides?.FirstDropDelaySeconds ?? global.FirstDropDelaySeconds,
        RespawnTimeBySeconds = map?.Overrides?.RespawnTimeBySeconds ?? global.RespawnTimeBySeconds,
        TimeSpreadBySeconds = map?.Overrides?.TimeSpreadBySeconds ?? global.TimeSpreadBySeconds,
        MaxCountTogether = map?.Overrides?.MaxCountTogether ?? map?.MaxCountTogether ?? global.MaxCountTogether,
        ChanceDrop = map?.Overrides?.ChanceDrop ?? map?.ChanceDrop ?? global.ChanceDrop,
        BoxesPerWave = map?.Overrides?.BoxesPerWave ?? global.BoxesPerWave,
        MaxDropsPerRound = map?.Overrides?.MaxDropsPerRound ?? global.MaxDropsPerRound,
        MaxDropsPerMap = map?.Overrides?.MaxDropsPerMap ?? global.MaxDropsPerMap,
        StartFromRound = map?.Overrides?.StartFromRound ?? global.StartFromRound,
        EveryNthRound = map?.Overrides?.EveryNthRound ?? global.EveryNthRound,
        MinPlayers = map?.Overrides?.MinPlayers ?? global.MinPlayers,
        MinAliveHumans = map?.Overrides?.MinAliveHumans ?? global.MinAliveHumans,
        MinAliveZombies = map?.Overrides?.MinAliveZombies ?? global.MinAliveZombies,
        CountBots = map?.Overrides?.CountBots ?? global.CountBots,
        AllowSurvivorRound = map?.Overrides?.AllowSurvivorRound ?? global.AllowSurvivorRound,
        AllowNemesisRound = map?.Overrides?.AllowNemesisRound ?? global.AllowNemesisRound,
        HumansCanCollect = map?.Overrides?.HumansCanCollect ?? global.HumansCanCollect,
        ZombiesCanCollect = map?.Overrides?.ZombiesCanCollect ?? global.ZombiesCanCollect,
        DropHeight = map?.Overrides?.DropHeight ?? global.DropHeight,
        FallSpeed = map?.Overrides?.FallSpeed ?? global.FallSpeed,
        PickupRadius = map?.Overrides?.PickupRadius ?? global.PickupRadius,
        LifetimeSeconds = map?.Overrides?.LifetimeSeconds ?? global.LifetimeSeconds,
        MaxCollectionsPerPlayerPerRound = map?.Overrides?.MaxCollectionsPerPlayerPerRound ?? global.MaxCollectionsPerPlayerPerRound,
        PlayerCooldownSeconds = map?.Overrides?.PlayerCooldownSeconds ?? global.PlayerCooldownSeconds,
        HealthCap = map?.Overrides?.HealthCap ?? global.HealthCap,
        ArmorCap = map?.Overrides?.ArmorCap ?? global.ArmorCap,
        SupplyBoxModel = global.SupplyBoxModel,
        ParachuteModel = map?.Overrides?.ParachuteModel ?? global.ParachuteModel,
        ParachuteSound = map?.Overrides?.ParachuteSound ?? global.ParachuteSound,
        DropSoundEvents = [.. (map?.Overrides?.DropSoundEvents ?? global.DropSoundEvents)],
    };
}

internal sealed class SupplyBoxRadar
{
    [StringLength(64)] public string ImageId { get; set; } = "";
    [StringLength(255)] public string ImageName { get; set; } = "";
    [StringLength(128)] public string OverviewName { get; set; } = "";
    public bool Calibrated { get; set; }
    [Range(-65536d, 65536d)] public double PosX { get; set; }
    [Range(-65536d, 65536d)] public double PosY { get; set; }
    [Range(0.001d, 4096d)] public double Scale { get; set; } = 1;
    [Range(128, 8192)] public int CoordinateSize { get; set; } = 1024;
    [Range(0, 270)] public int Rotation { get; set; }
}
