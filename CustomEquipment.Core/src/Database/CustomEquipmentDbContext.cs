using CustomEquipment.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomEquipment.Database;

public sealed class CustomEquipmentDbContext(DbContextOptions<CustomEquipmentDbContext> options) : DbContext(options)
{
    public const string SchemaName = "custom_equipment";

    internal DbSet<WeaponEntity> Weapons => Set<WeaponEntity>();

    internal DbSet<WeaponSoundEntity> WeaponSounds => Set<WeaponSoundEntity>();

    internal DbSet<WeaponSoundFileEntity> WeaponSoundFiles => Set<WeaponSoundFileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        ConfigureWeapons(modelBuilder);
        ConfigureSounds(modelBuilder);
        ConfigureSoundFiles(modelBuilder);
    }

    private static void ConfigureWeapons(ModelBuilder modelBuilder)
    {
        var weapons = modelBuilder.Entity<WeaponEntity>();

        weapons.HasIndex(x => x.InternalName).IsUnique();
        weapons.HasIndex(x => new { x.Enabled, x.SortOrder });

        weapons.Property(x => x.AccessFlags).HasDefaultValue((short)1);
        weapons.Property(x => x.Rarity).HasDefaultValue("Common");
        weapons.Property(x => x.Enabled).HasDefaultValue(true);
        weapons.Property(x => x.SortOrder).HasDefaultValue(0);
        weapons.Property(x => x.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
        weapons.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");

        weapons.ToTable(
            "weapons",
            SchemaName,
            table =>
            {
                table.HasCheckConstraint("CK_weapons_access_flags", "access_flags >= 0 AND access_flags <= 3");
                table.HasCheckConstraint("CK_weapons_item_price", "item_price >= 0");
                table.HasCheckConstraint("CK_weapons_ammo_price", "ammo_price IS NULL OR ammo_price >= 0");
                table.HasCheckConstraint("CK_weapons_ammunition", "(clip_size IS NULL OR clip_size >= 0) AND (reserve_ammo IS NULL OR reserve_ammo >= 0)");
                table.HasCheckConstraint("CK_weapons_timing", "(cycle_time_primary IS NULL OR cycle_time_primary > 0) AND (cycle_time_secondary IS NULL OR (cycle_time_secondary > 0 AND cycle_time_primary IS NOT NULL)) AND (deploy_duration IS NULL OR deploy_duration >= 0)");
                table.HasCheckConstraint("CK_weapons_ballistics", "(num_bullets IS NULL OR num_bullets >= 1) AND (penetration IS NULL OR penetration >= 0) AND (effective_range IS NULL OR effective_range >= 0) AND (range_modifier IS NULL OR range_modifier >= 0)");
                table.HasCheckConstraint("CK_weapons_damage", "(damage_head IS NULL OR damage_head >= 0) AND (damage_chest IS NULL OR damage_chest >= 0) AND (damage_stomach IS NULL OR damage_stomach >= 0) AND (damage_left_arm IS NULL OR damage_left_arm >= 0) AND (damage_right_arm IS NULL OR damage_right_arm >= 0) AND (damage_left_leg IS NULL OR damage_left_leg >= 0) AND (damage_right_leg IS NULL OR damage_right_leg >= 0) AND (damage_neck IS NULL OR damage_neck >= 0)");
            }
        );
    }

    private static void ConfigureSounds(ModelBuilder modelBuilder)
    {
        var sounds = modelBuilder.Entity<WeaponSoundEntity>();

        sounds.HasIndex(x => x.EventName).IsUnique();
        sounds.HasIndex(x => new { x.WeaponId, x.Trigger }).IsUnique();

        sounds.Property(x => x.SoundType).HasDefaultValue("csgo_mega");
        sounds.Property(x => x.Volume).HasDefaultValue(1.0f);
        sounds.Property(x => x.Pitch).HasDefaultValue(1.0f);
        sounds.Property(x => x.MixGroup).HasDefaultValue("Weapons");
        sounds.Property(x => x.PreloadVsnds).HasDefaultValue(true);
        sounds.Property(x => x.Enabled).HasDefaultValue(true);
        sounds.Property(x => x.SortOrder).HasDefaultValue(0);
        sounds.Property(x => x.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
        sounds.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");

        sounds
            .HasOne(x => x.Weapon)
            .WithMany(x => x.Sounds)
            .HasForeignKey(x => x.WeaponId)
            .OnDelete(DeleteBehavior.Cascade);

        sounds.ToTable(
            "weapon_sounds",
            SchemaName,
            table =>
            {
                table.HasCheckConstraint("CK_weapon_sounds_volume", "volume >= 0");
                table.HasCheckConstraint("CK_weapon_sounds_pitch", "pitch > 0");
            }
        );
    }

    private static void ConfigureSoundFiles(ModelBuilder modelBuilder)
    {
        var files = modelBuilder.Entity<WeaponSoundFileEntity>();

        files.HasIndex(x => new { x.SoundId, x.Track, x.SortOrder });
        files.Property(x => x.Track).HasDefaultValue(1);
        files.Property(x => x.SortOrder).HasDefaultValue(0);

        files
            .HasOne(x => x.Sound)
            .WithMany(x => x.Files)
            .HasForeignKey(x => x.SoundId)
            .OnDelete(DeleteBehavior.Cascade);

        files.ToTable(
            "weapon_sound_files",
            SchemaName,
            table => table.HasCheckConstraint("CK_weapon_sound_files_track", "track >= 1 AND track <= 99")
        );
    }
}
