using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Advertisement.Core.Database.Entities;

[Table("player_preferences", Schema = AdvertisementDbContext.CoreSchemaName)]
internal sealed class PlayerPreferenceEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("steam_id")]
    public long SteamId { get; set; }

    [MaxLength(16)]
    [Column("locale")]
    public string? Locale { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
