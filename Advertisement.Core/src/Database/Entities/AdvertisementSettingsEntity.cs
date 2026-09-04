using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advertisement.Core.Database.Entities;

[Table("settings", Schema = AdvertisementDbContext.SchemaName)]
internal sealed class AdvertisementSettingsEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [MaxLength(16)]
    [Column("default_locale")]
    public string DefaultLocale { get; set; } = "ru";

    [Column("allowed_locales", TypeName = "jsonb")]
    public string AllowedLocalesJson { get; set; } = "[\"ru\",\"en\",\"uk\",\"pl\",\"de\"]";

    [Column("interval_seconds")]
    public int IntervalSeconds { get; set; } = 90;

    [Column("refresh_interval_seconds")]
    public int RefreshIntervalSeconds { get; set; } = 30;

    [Column("initial_delay_seconds")]
    public int InitialDelaySeconds { get; set; } = 45;

    [MaxLength(32)]
    [Column("order_mode")]
    public string OrderMode { get; set; } = "sequential";

    [Column("exclude_bots_from_players")]
    public bool ExcludeBotsFromPlayers { get; set; } = true;

    [Column("configuration_version")]
    public long ConfigurationVersion { get; set; } = 1;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
