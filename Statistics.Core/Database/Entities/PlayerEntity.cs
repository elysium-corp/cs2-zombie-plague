using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Statistics.Core.Database.Entities;

[Table("players", Schema = StatisticsDbContext.SchemaName)]
internal sealed class PlayerEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("steam_id")]
    public long SteamId { get; set; }

    [Required]
    [MaxLength(128)]
    [Column("last_known_name")]
    public string LastKnownName { get; set; } = string.Empty;

    [Column("first_seen_at")]
    public DateTime FirstSeenAtUtc { get; set; }

    [Column("last_seen_at")]
    public DateTime LastSeenAtUtc { get; set; }

    public PlayerStatisticsEntity? Statistics { get; set; }
}

