using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Localization.Core.Database.Entities;

[Index(nameof(LanguageCode), Name = "translations_language_idx")]
[Table("translations", Schema = LocalizationDbContext.SchemaName)]
internal sealed class LocalizationTranslationEntity
{
    [Column("entry_id")]
    public long EntryId { get; set; }

    [MaxLength(16)]
    [Column("language_code")]
    public string LanguageCode { get; set; } = string.Empty;

    [Column("text")]
    public string Text { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public LocalizationEntryEntity Entry { get; set; } = null!;
    public LocalizationLanguageEntity Language { get; set; } = null!;
}
