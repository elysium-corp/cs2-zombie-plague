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

        statistics.Property(x => x.SessionsCount).HasDefaultValue(0L);
        statistics.Property(x => x.PlayTimeSeconds).HasDefaultValue(0L);
        statistics.Property(x => x.RoundsPlayed).HasDefaultValue(0L);
        statistics.Property(x => x.RoundsAsHuman).HasDefaultValue(0L);
        statistics.Property(x => x.RoundsAsZombie).HasDefaultValue(0L);
        statistics.Property(x => x.ZombiesKilled).HasDefaultValue(0L);
        statistics.Property(x => x.HeadshotZombieKills).HasDefaultValue(0L);
        statistics.Property(x => x.InfectionsMade).HasDefaultValue(0L);
        statistics.Property(x => x.TimesInfected).HasDefaultValue(0L);
        statistics.Property(x => x.DeathsAsHuman).HasDefaultValue(0L);
        statistics.Property(x => x.DeathsAsZombie).HasDefaultValue(0L);
        statistics.Property(x => x.DamageToZombies).HasDefaultValue(0L);
        statistics.Property(x => x.DamageToHumans).HasDefaultValue(0L);
        statistics.Property(x => x.SurvivedRounds).HasDefaultValue(0L);
        statistics.Property(x => x.HumanWins).HasDefaultValue(0L);
        statistics.Property(x => x.ZombieWins).HasDefaultValue(0L);
        statistics.Property(x => x.FirstZombieRounds).HasDefaultValue(0L);
        statistics.Property(x => x.LastHumanRounds).HasDefaultValue(0L);
        statistics.Property(x => x.LastHumanSurvivals).HasDefaultValue(0L);
        statistics.Property(x => x.BestKillStreak).HasDefaultValue(0L);
        statistics.Property(x => x.BestInfectionStreak).HasDefaultValue(0L);

        statistics
            .Property(x => x.UpdatedAtUtc)
            .HasDefaultValueSql(PostgreSqlCurrentTimestamp);
    }
}

