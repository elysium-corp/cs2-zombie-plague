using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advertisement.Core.Database.Entities;

[Table("banner_templates", Schema = AdvertisementDbContext.SchemaName)]
internal sealed class BannerTemplateEntity
{
    [Key, MaxLength(64), Column("key")]
    public string Key { get; set; } = string.Empty;
    [MaxLength(128), Column("name")]
    public string Name { get; set; } = string.Empty;
    [Column("design", TypeName = "jsonb")]
    public string DesignJson { get; set; } = "{}";
    [Column("sound_preview_url")]
    public string? SoundPreviewUrl { get; set; }
    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
