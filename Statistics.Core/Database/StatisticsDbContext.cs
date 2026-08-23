using Microsoft.EntityFrameworkCore;
using Statistics.Core.Database.Entities;

namespace Statistics.Core.Database;

public sealed class StatisticsDbContext(DbContextOptions<StatisticsDbContext> options) : DbContext(options)
{
    public const string SchemaName = "statistics";

    internal DbSet<PlayerEntity> Players => Set<PlayerEntity>();

    internal DbSet<PlayerStatisticsEntity> PlayerStatistics => Set<PlayerStatisticsEntity>();

    private const string PostgreSqlCurrentTimestamp = "CURRENT_TIMESTAMP";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        ConfigurePlayers(modelBuilder);
        ConfigurePlayerStatistics(modelBuilder);
    }

    private static void ConfigurePlayers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerEntity>()
            .Property(x => x.FirstSeenAtUtc)
            .HasDefaultValueSql(PostgreSqlCurrentTimestamp);

        modelBuilder.Entity<PlayerEntity>()
            .Property(x => x.LastSeenAtUtc)
            .HasDefaultValueSql(PostgreSqlCurrentTimestamp);
    }

    private static void ConfigurePlayerStatistics(ModelBuilder modelBuilder)
    {
        var statistics = modelBuilder.Entity<PlayerStatisticsEntity>();

        statistics
            .HasOne(x => x.Player)
            .WithOne(x => x.Statistics)
            .HasForeignKey<PlayerStatisticsEntity>(x => x.SteamId)
            .OnDelete(DeleteBehavior.Cascade);

        statistics.ToTable(
            "player_statistics",
            SchemaName,
            table => table.HasCheckConstraint(
                "CK_player_statistics_points_non_negative",
                "points >= 0"
            )
        );

        statistics.Property(x => x.Points).HasDefaultValue(0L);
        statistics.Property(x => x.PlayTimeSeconds).HasDefaultValue(0L);
        statistics.Property(x => x.ZombiesKilled).HasDefaultValue(0L);
        statistics.Property(x => x.InfectionsMade).HasDefaultValue(0L);
        statistics.Property(x => x.TimesInfected).HasDefaultValue(0L);
        statistics.Property(x => x.Deaths).HasDefaultValue(0L);
        statistics.Property(x => x.HumanWins).HasDefaultValue(0L);
        statistics.Property(x => x.ZombieWins).HasDefaultValue(0L);
        statistics.Property(x => x.BestKillStreak).HasDefaultValue(0L);
        statistics.Property(x => x.BestInfectionStreak).HasDefaultValue(0L);

        statistics
            .Property(x => x.UpdatedAtUtc)
            .HasDefaultValueSql(PostgreSqlCurrentTimestamp);
    }
}
