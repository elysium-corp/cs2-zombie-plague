using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Advertisement.Core.Database.Entities;

[Index(nameof(Locale), Name = "tag_translations_locale_idx")]
[Table("tag_translations", Schema = AdvertisementDbContext.SchemaName)]
internal sealed class AdvertisementTagTranslationEntity
{
    [Column("tag_id")]
    public long TagId { get; set; }

    [MaxLength(16)]
    [Column("locale")]
    public string Locale { get; set; } = string.Empty;

    [MaxLength(64)]
    [Column("text")]
    public string Text { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public AdvertisementTagEntity Tag { get; set; } = null!;
}
