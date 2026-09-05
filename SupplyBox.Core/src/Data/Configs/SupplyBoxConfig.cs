using System.ComponentModel.DataAnnotations;

namespace SupplyBox.Data.Configs;

public class SupplyBoxConfig : ISupplyBoxConfig
{
    public bool Enabled { get; set; } = true;
    [Range(1, 3600)] public int FirstDropDelaySeconds { get; set; } = 30;
    [Range(1, 3600)] public int RespawnTimeBySeconds { get; set; } = 120;
    [Range(0, 3600)] public int TimeSpreadBySeconds { get; set; } = 30;
    [Range(1, 32)] public int MaxCountTogether { get; set; } = 2;
    [Range(0, 100)] public int ChanceDrop { get; set; } = 100;
    [Range(1, 16)] public int BoxesPerWave { get; set; } = 1;
    [Range(0, 1000)] public int MaxDropsPerRound { get; set; } = 0;
    [Range(0, 10000)] public int MaxDropsPerMap { get; set; } = 0;
    [Range(1, 1000)] public int StartFromRound { get; set; } = 1;
    [Range(1, 1000)] public int EveryNthRound { get; set; } = 1;
    [Range(0, 64)] public int MinPlayers { get; set; } = 1;
    [Range(0, 64)] public int MinAliveHumans { get; set; } = 1;
    [Range(0, 64)] public int MinAliveZombies { get; set; } = 0;
    public bool CountBots { get; set; } = false;
    public bool AllowSurvivorRound { get; set; } = false;
    public bool AllowNemesisRound { get; set; } = false;
    public bool HumansCanCollect { get; set; } = true;
    public bool ZombiesCanCollect { get; set; } = false;
    public bool AutoDiscoverSpawnPoints { get; set; } = true;
    [Range(0, 4096)] public int DropHeight { get; set; } = 600;
    [Range(10, 1000)] public int FallSpeed { get; set; } = 160;
    [Range(16, 256)] public int PickupRadius { get; set; } = 60;
    [Range(0, 3600)] public int LifetimeSeconds { get; set; } = 180;
    [Range(0, 1000)] public int MaxCollectionsPerPlayerPerRound { get; set; } = 0;
    [Range(0, 3600)] public int PlayerCooldownSeconds { get; set; } = 0;
    [Range(1, 100000)] public int HealthCap { get; set; } = 100;
    [Range(1, 1000)] public int ArmorCap { get; set; } = 100;
    public string SupplyBoxModel { get; set; } = "models/props/crates/cs2_drop_crate_01.vmdl";
    public string ParachuteModel { get; set; } = "";
    [StringLength(128)] public string ParachuteSound { get; set; } = "";
}
