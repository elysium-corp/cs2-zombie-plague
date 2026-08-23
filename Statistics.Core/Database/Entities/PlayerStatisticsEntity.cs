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

    [Column("sessions_count")]
    public long SessionsCount { get; set; }

    [Column("play_time_seconds")]
    public long PlayTimeSeconds { get; set; }

    [Column("rounds_played")]
    public long RoundsPlayed { get; set; }

    [Column("rounds_as_human")]
    public long RoundsAsHuman { get; set; }

    [Column("rounds_as_zombie")]
    public long RoundsAsZombie { get; set; }

    [Column("zombies_killed")]
    public long ZombiesKilled { get; set; }

    [Column("headshot_zombie_kills")]
    public long HeadshotZombieKills { get; set; }

    [Column("infections_made")]
    public long InfectionsMade { get; set; }

    [Column("times_infected")]
    public long TimesInfected { get; set; }

    [Column("deaths_as_human")]
    public long DeathsAsHuman { get; set; }

    [Column("deaths_as_zombie")]
    public long DeathsAsZombie { get; set; }

    [Column("damage_to_zombies")]
    public long DamageToZombies { get; set; }

    [Column("damage_to_humans")]
    public long DamageToHumans { get; set; }

    [Column("survived_rounds")]
    public long SurvivedRounds { get; set; }

    [Column("human_wins")]
    public long HumanWins { get; set; }

    [Column("zombie_wins")]
    public long ZombieWins { get; set; }

    [Column("first_zombie_rounds")]
    public long FirstZombieRounds { get; set; }

    [Column("last_human_rounds")]
    public long LastHumanRounds { get; set; }

    [Column("last_human_survivals")]
    public long LastHumanSurvivals { get; set; }

    [Column("best_kill_streak")]
    public long BestKillStreak { get; set; }

    [Column("best_infection_streak")]
    public long BestInfectionStreak { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAtUtc { get; set; }
}

