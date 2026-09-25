using MapRotation.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace MapRotation.Core.Database;

internal sealed class MapRotationDbContext(DbContextOptions<MapRotationDbContext> options) : DbContext(options)
{
    public const string SchemaName = "map_rotation";
    public DbSet<RotationSettings> Settings => Set<RotationSettings>();
    public DbSet<RotationMap> Maps => Set<RotationMap>();
    public DbSet<RotationHudPreferenceEntity> PlayerPreferences => Set<RotationHudPreferenceEntity>();
    public DbSet<RuntimeEntity> Runtime => Set<RuntimeEntity>();
    public DbSet<HistoryEntity> History => Set<HistoryEntity>();
    public DbSet<VoteEntity> Votes => Set<VoteEntity>();
    public DbSet<VoteOptionEntity> Options => Set<VoteOptionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.Entity<RotationSettings>().ToTable("settings").HasKey(x => x.Id);
        modelBuilder.Entity<RotationSettings>().Property(x => x.Id).ValueGeneratedNever();
        modelBuilder.Entity<RotationSettings>().HasData(new RotationSettings());
        modelBuilder.Entity<RotationSettings>().Property(x => x.HudSettings).HasColumnType("jsonb");
        var maps = modelBuilder.Entity<RotationMap>();
        maps.ToTable("maps"); maps.HasKey(x => x.Id);
        maps.Ignore(x => x.IsSafe); maps.Ignore(x => x.EngineTarget);
        maps.Property(x => x.Key).HasMaxLength(64); maps.HasIndex(x => x.Key).IsUnique();
        maps.Property(x => x.MapName).HasMaxLength(128); maps.HasIndex(x => x.MapName).IsUnique();
        maps.Property(x => x.DisplayName).HasMaxLength(128);
        maps.Property(x => x.HudImagePath).HasMaxLength(256);
        maps.Property(x => x.Enabled).HasDefaultValue(true);
        maps.Property(x => x.Weight).HasDefaultValue(1d);
        maps.Property(x => x.AllowNomination).HasDefaultValue(true);
        maps.Property(x => x.AllowVote).HasDefaultValue(true);
        maps.Property(x => x.AllowAutoRotation).HasDefaultValue(true);
        maps.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        maps.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        var preferences = modelBuilder.Entity<RotationHudPreferenceEntity>();
        preferences.ToTable("player_preferences", table =>
        {
            table.HasCheckConstraint("ck_player_preferences_orientation", "orientation IN ('horizontal', 'vertical')");
            table.HasCheckConstraint("ck_player_preferences_hud_scale", "hud_scale IN (80, 100, 120)");
        });
        preferences.HasKey(x => x.SteamId);
        preferences.Property(x => x.SteamId).ValueGeneratedNever();
        preferences.Property(x => x.Orientation).HasMaxLength(10).HasDefaultValue("horizontal");
        preferences.Property(x => x.HudScale).HasDefaultValue(100);
        var runtime = modelBuilder.Entity<RuntimeEntity>();
        runtime.ToTable("runtime_state"); runtime.HasKey(x => x.Id);
        runtime.Property(x => x.Id).ValueGeneratedNever();
        runtime.Property(x => x.Checkpoint).HasColumnType("jsonb");
        var history = modelBuilder.Entity<HistoryEntity>();
        history.ToTable("map_history"); history.HasKey(x => x.Id);
        history.HasIndex(x => x.EndedAt);
        var votes = modelBuilder.Entity<VoteEntity>();
        votes.ToTable("vote_sessions"); votes.HasKey(x => x.Id);
        votes.HasIndex(x => x.StartedAt);
        var options = modelBuilder.Entity<VoteOptionEntity>();
        options.ToTable("vote_options"); options.HasKey(x => new { x.VoteId, x.MapId });
        options.HasOne<VoteEntity>().WithMany().HasForeignKey(x => x.VoteId).OnDelete(DeleteBehavior.Cascade);
        // Снимки названий сохраняются после удаления карты из каталога.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                property.SetColumnName(System.Text.RegularExpressions.Regex.Replace(property.Name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant());
    }
}

internal sealed class RuntimeEntity
{
    public int Id { get; set; } = 1;
    public string Checkpoint { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; }
}
internal sealed class HistoryEntity
{
    public Guid Id { get; set; }
    public long? MapId { get; set; }
    public string MapName { get; set; } = "";
    public string WorkshopId { get; set; } = "";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
}
internal sealed class VoteEntity
{
    public Guid Id { get; set; }
    public string Source { get; set; } = "";
    public string CurrentMap { get; set; } = "";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public long? WinnerMapId { get; set; }
}
internal sealed class VoteOptionEntity
{
    public Guid VoteId { get; set; }
    public long MapId { get; set; }
    public string MapName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int Votes { get; set; }
}

internal sealed class RotationHudPreferenceEntity
{
    public long SteamId { get; set; }
    public string Orientation { get; set; } = "horizontal";
    public int HudScale { get; set; } = 100;
}
