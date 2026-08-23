using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Statistics.Core.Database.Entities;

[Table("player_statistics", Schema = StatisticsDbContext.SchemaName)]
internal sealed class PlayerStatisticsEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("steam_id")]
    public long SteamId { get; set; }

    public PlayerEntity Player { get; set; } = null!;

    [Column("points")]
    public long Points { get; set; }

    [Column("play_time_seconds")]
    public long PlayTimeSeconds { get; set; }

    [Column("zombies_killed")]
    public long ZombiesKilled { get; set; }

    [Column("infections_made")]
    public long InfectionsMade { get; set; }

    [Column("times_infected")]
    public long TimesInfected { get; set; }

    [Column("deaths")]
    public long Deaths { get; set; }

    [Column("human_wins")]
    public long HumanWins { get; set; }

    [Column("zombie_wins")]
    public long ZombieWins { get; set; }

    [Column("best_kill_streak")]
    public long BestKillStreak { get; set; }

    [Column("best_infection_streak")]
    public long BestInfectionStreak { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }
}
