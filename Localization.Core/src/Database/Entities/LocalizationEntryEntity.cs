using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Localization.Core.Database.Entities;

[Index(nameof(Key), Name = "entries_key_unique", IsUnique = true)]
[Table("entries", Schema = LocalizationDbContext.SchemaName)]
internal sealed class LocalizationEntryEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [MaxLength(191)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [MaxLength(512)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("is_critical")]
    public bool IsCritical { get; set; }

    [Column("parameters", TypeName = "jsonb")]
    public string ParametersJson { get; set; } = "[]";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<LocalizationTranslationEntity> Translations { get; set; } = [];
}
