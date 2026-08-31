using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Localization.Core.Database.Entities;

[Index(nameof(Code), Name = "languages_code_unique", IsUnique = true)]
[Table("languages", Schema = LocalizationDbContext.SchemaName)]
internal sealed class LocalizationLanguageEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [MaxLength(16)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [MaxLength(64)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(64)]
    [Column("native_name")]
    public string NativeName { get; set; } = string.Empty;

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<LocalizationTranslationEntity> Translations { get; set; } = [];
}
