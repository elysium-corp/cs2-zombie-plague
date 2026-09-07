using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advertisement.Core.Database.Entities;

[Table("messages", Schema = AdvertisementDbContext.SchemaName)]
internal sealed class AdvertisementMessageEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [MaxLength(64)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [MaxLength(128)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(191)]
    [Column("localization_key")]
    public string LocalizationKey { get; set; } = string.Empty;

    [MaxLength(64)]
    [Column("tag_key")]
    public string? TagKey { get; set; }

    [MaxLength(32)]
    [Column("type")]
    public string Type { get; set; } = "information";

    [MaxLength(32)]
    [Column("display_type")]
    public string DisplayType { get; set; } = "chat";

    [MaxLength(191)]
    [Column("hud_localization_key")]
    public string? HudLocalizationKey { get; set; }

    [MaxLength(16)]
    [Column("hud_position")]
    public string HudPosition { get; set; } = "bottom_left";

    [Column("hud_duration_seconds")]
    public double HudDurationSeconds { get; set; } = 8;

    [MaxLength(16)]
    [Column("hud_style")]
    public string HudStyle { get; set; } = "notice";

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("priority")]
    public int Priority { get; set; }

    [Column("weight")]
    public int Weight { get; set; } = 100;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("interval_seconds")]
    public int? IntervalSeconds { get; set; }

    [MaxLength(16)]
    [Column("dispatch_mode")]
    public string DispatchMode { get; set; } = "periodic";

    [Column("daily_times", TypeName = "jsonb")]
    public string DailyTimesJson { get; set; } = "[]";

    [Column("daily_start_time")]
    public TimeOnly? DailyStartTime { get; set; }

    [Column("daily_end_time")]
    public TimeOnly? DailyEndTime { get; set; }

    [MaxLength(16)]
    [Column("audience_type")]
    public string AudienceType { get; set; } = "all";

    [MaxLength(64)]
    [Column("audience_group")]
    public string? AudienceGroup { get; set; }

    [Column("min_players")]
    public int? MinPlayers { get; set; }

    [Column("max_players")]
    public int? MaxPlayers { get; set; }

    [Column("starts_at")]
    public DateTimeOffset? StartsAt { get; set; }

    [Column("ends_at")]
    public DateTimeOffset? EndsAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<AdvertisementMessageTranslationEntity> Translations { get; set; } = [];
}
