using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Localization.Core.Database.Entities;

[Table("player_preferences", Schema = LocalizationDbContext.SchemaName)]
internal sealed class PlayerLanguagePreferenceEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("steam_id")]
    public long SteamId { get; set; }

    [MaxLength(16)]
    [Column("language_code")]
    public string LanguageCode { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
