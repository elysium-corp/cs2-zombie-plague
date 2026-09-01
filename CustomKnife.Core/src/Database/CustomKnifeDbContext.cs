using CustomKnife.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomKnife.Database;

internal sealed class CustomKnifeDbContext(DbContextOptions<CustomKnifeDbContext> options) : DbContext(options)
{
    public const string SchemaName = "custom_knife";

    public DbSet<PlayerKnifeEntity> PlayerKnives => Set<PlayerKnifeEntity>();

    public DbSet<KnifeEntity> Knives => Set<KnifeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        base.OnModelCreating(modelBuilder);

        var knives = modelBuilder.Entity<KnifeEntity>();

        knives.Property(x => x.Enabled).HasDefaultValue(true);
        knives.Property(x => x.SortOrder).HasDefaultValue(0);
        knives.Property(x => x.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
        knives.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");

        knives.ToTable(
            "knives",
            SchemaName,
            table =>
            {
                table.HasCheckConstraint("CK_knives_speed", "speed >= 1 AND speed <= 2000");
                table.HasCheckConstraint(
                    "CK_knives_knockback",
                    "knockback_recoil >= 0 AND knockback_recoil <= 100000 " +
                    "AND knockback_pick_distance >= 0 AND knockback_pick_distance <= 100000"
                );
                table.HasCheckConstraint("CK_knives_gravity", "gravity >= 1 AND gravity <= 10000");
                table.HasCheckConstraint(
                    "CK_knives_damage_multiplier",
                    "damage_multiplier >= 0 AND damage_multiplier <= 1000"
                );
                table.HasCheckConstraint(
                    "CK_knives_required_permission",
                    "required_permission IS NULL OR required_permission ~ '^[a-z0-9_.:-]+$'"
                );
            }
        );
    }
}
