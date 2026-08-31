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

    [Column("tag_id")]
    public long? TagId { get; set; }

    [MaxLength(32)]
    [Column("type")]
    public string Type { get; set; } = "information";

    [MaxLength(32)]
    [Column("display_type")]
    public string DisplayType { get; set; } = "chat";

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

    public AdvertisementTagEntity? Tag { get; set; }
    public ICollection<AdvertisementMessageTranslationEntity> Translations { get; set; } = [];
}
