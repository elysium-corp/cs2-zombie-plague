using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Advertisement.Core.Database.Entities;

[Index(nameof(Key), Name = "tags_key_key", IsUnique = true)]
[Table("tags", Schema = AdvertisementDbContext.SchemaName)]
internal sealed class AdvertisementTagEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [MaxLength(64)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

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

    public ICollection<AdvertisementTagTranslationEntity> Translations { get; set; } = [];
    public ICollection<AdvertisementMessageEntity> Messages { get; set; } = [];
}
