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
                    "CK_knives_localization_keys",
                    "display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\\.[A-Z0-9][A-Za-z0-9]*)*$' AND description_key ~ '^[A-Z0-9][A-Za-z0-9]*(\\.[A-Z0-9][A-Za-z0-9]*)*$'"
                );
                table.HasCheckConstraint(
                    "CK_knives_image_url",
                    "image_url IS NULL OR image_url ~ '^https://[^[:space:]]+$' OR image_url ~ '^assets/uploads/elysium-equipments/items/[a-f0-9]{40}\\.(jpg|jpeg|png|webp|avif)$'"
                );
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
