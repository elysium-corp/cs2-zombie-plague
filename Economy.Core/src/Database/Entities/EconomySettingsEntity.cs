using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Economy.Core.Database.Entities;

[Table("settings", Schema = EconomyDbContext.SchemaName)]
internal sealed class EconomySettingsEntity
{
    [Key]
    [Column("id")]
    public short Id { get; set; } = 1;

    [Column("revision")]
    public long Revision { get; set; }

    [Column("absolute_max_money")]
    public int AbsoluteMaxMoney { get; set; } = 99_999;

    [Column("default_max_money")]
    public int DefaultMaxMoney { get; set; } = 99_999;

    [Column("start_money")]
    public int StartMoney { get; set; } = 5_000;

    [Column("money_per_damage", TypeName = "numeric(12,4)")]
    public decimal MoneyPerDamage { get; set; } = 0.5m;

    [Column("money_for_infection")]
    public int MoneyForInfection { get; set; } = 500;

    [Column("money_for_zombie_kill")]
    public int MoneyForZombieKill { get; set; }

    [Column("money_for_human_kill")]
    public int MoneyForHumanKill { get; set; }

    [Column("save_on_round_end")]
    public bool SaveOnRoundEnd { get; set; }

    [Column("save_on_disconnect")]
    public bool SaveOnDisconnect { get; set; } = true;

    [Column("save_on_unload")]
    public bool SaveOnUnload { get; set; } = true;

    [Column("periodic_save_enabled")]
    public bool PeriodicSaveEnabled { get; set; }

    [Column("periodic_save_interval_seconds")]
    public int PeriodicSaveIntervalSeconds { get; set; } = 300;

    [Column("settings_refresh_interval_seconds")]
    public int SettingsRefreshIntervalSeconds { get; set; } = 30;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
