using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Localization.Core.Database.Entities;

[Index(nameof(Key), Name = "tags_key_unique", IsUnique = true)]
[Index(nameof(LocalizationKey), Name = "tags_localization_key_unique", IsUnique = true)]
[Table("tags", Schema = LocalizationDbContext.SchemaName)]
internal sealed class LocalizationTagEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [MaxLength(64)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [MaxLength(191)]
    [Column("localization_key")]
    public string LocalizationKey { get; set; } = string.Empty;

    [MaxLength(32)]
    [Column("color")]
    public string Color { get; set; } = "default";

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
