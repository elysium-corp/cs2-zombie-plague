using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Localization.Core.Database.Entities;

[Table("settings", Schema = LocalizationDbContext.SchemaName)]
internal sealed class LocalizationSettingsEntity
{
    [Key]
    [Column("id")]
    public short Id { get; set; } = 1;

    [MaxLength(16)]
    [Column("server_fallback_language")]
    public string ServerFallbackLanguage { get; set; } = "ru";

    [Column("refresh_interval_seconds")]
    public int RefreshIntervalSeconds { get; set; } = 30;

    [Column("local_cache_enabled")]
    public bool LocalCacheEnabled { get; set; } = true;

    [Column("log_missing_keys")]
    public bool LogMissingKeys { get; set; } = true;

    [Column("color_tags")]
    public string ColorTagsJson { get; set; } =
        "{\"default\":\"default\",\"accent\":\"lightblue\",\"warning\":\"red\",\"success\":\"green\",\"important\":\"orange\",\"muted\":\"gray\"}";

    [Column("configuration_version")]
    public long ConfigurationVersion { get; set; } = 1;

    [Column("fallback_exported_version")]
    public long FallbackExportedVersion { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
