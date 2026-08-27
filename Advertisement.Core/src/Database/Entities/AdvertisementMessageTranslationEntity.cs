using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Advertisement.Core.Database.Entities;

[Index(nameof(Locale), Name = "message_translations_locale_idx")]
[Table("message_translations", Schema = AdvertisementDbContext.SchemaName)]
internal sealed class AdvertisementMessageTranslationEntity
{
    [Column("message_id")]
    public long MessageId { get; set; }

    [MaxLength(16)]
    [Column("locale")]
    public string Locale { get; set; } = string.Empty;

    [Column("text")]
    public string Text { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public AdvertisementMessageEntity Message { get; set; } = null!;
}
