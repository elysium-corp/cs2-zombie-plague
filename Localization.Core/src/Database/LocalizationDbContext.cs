using Localization.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Localization.Core.Database;

internal sealed class LocalizationDbContext(DbContextOptions<LocalizationDbContext> options) : DbContext(options)
{
    public const string SchemaName = "localization";

    internal DbSet<LocalizationLanguageEntity> Languages => Set<LocalizationLanguageEntity>();
    internal DbSet<LocalizationSettingsEntity> Settings => Set<LocalizationSettingsEntity>();
    internal DbSet<LocalizationEntryEntity> Entries => Set<LocalizationEntryEntity>();
    internal DbSet<LocalizationTranslationEntity> Translations => Set<LocalizationTranslationEntity>();
    internal DbSet<LocalizationTagEntity> Tags => Set<LocalizationTagEntity>();
    internal DbSet<PlayerLanguagePreferenceEntity> PlayerPreferences => Set<PlayerLanguagePreferenceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);

        var language = modelBuilder.Entity<LocalizationLanguageEntity>();
        language.Property(entity => entity.Enabled).HasDefaultValue(true);
        language.Property(entity => entity.SortOrder).HasDefaultValue(0);

        var settings = modelBuilder.Entity<LocalizationSettingsEntity>();
        settings.Property(entity => entity.Id).ValueGeneratedNever();
        settings.Property(entity => entity.ServerFallbackLanguage).HasDefaultValue("ru");
        settings.Property(entity => entity.RefreshIntervalSeconds).HasDefaultValue(30);
        settings.Property(entity => entity.LocalCacheEnabled).HasDefaultValue(true);
        settings.Property(entity => entity.LogMissingKeys).HasDefaultValue(true);
        settings.Property(entity => entity.ColorTagsJson)
            .HasColumnType("jsonb")
            .HasDefaultValueSql(
                "'{\"default\":\"default\",\"accent\":\"lightblue\",\"warning\":\"red\",\"success\":\"green\",\"important\":\"orange\",\"muted\":\"gray\"}'::jsonb");
        settings.Property(entity => entity.ConfigurationVersion).HasDefaultValue(1L);
        settings.ToTable("settings", SchemaName, table =>
        {
            table.HasCheckConstraint("settings_singleton", "id = 1");
            table.HasCheckConstraint("settings_color_tags_object", "jsonb_typeof(color_tags) = 'object'");
        });

        var entry = modelBuilder.Entity<LocalizationEntryEntity>();
        entry.Property(entity => entity.IsCritical).HasDefaultValue(false);
        entry.Property(entity => entity.ParametersJson)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");
        entry.ToTable("entries", SchemaName, table =>
            table.HasCheckConstraint("entries_parameters_array", "jsonb_typeof(parameters) = 'array'"));

        var translation = modelBuilder.Entity<LocalizationTranslationEntity>();
        translation.HasKey(entity => new { entity.EntryId, entity.LanguageCode });
        translation.HasOne(entity => entity.Entry)
            .WithMany(entity => entity.Translations)
            .HasForeignKey(entity => entity.EntryId)
            .OnDelete(DeleteBehavior.Cascade);
        translation.HasOne(entity => entity.Language)
            .WithMany(entity => entity.Translations)
            .HasPrincipalKey(entity => entity.Code)
            .HasForeignKey(entity => entity.LanguageCode)
            .OnDelete(DeleteBehavior.Cascade);

        var tag = modelBuilder.Entity<LocalizationTagEntity>();
        tag.Property(entity => entity.Color).HasDefaultValue("default");
        tag.Property(entity => entity.Enabled).HasDefaultValue(true);
        tag.Property(entity => entity.SortOrder).HasDefaultValue(0);
        tag.Property(entity => entity.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        tag.Property(entity => entity.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        tag.ToTable("tags", SchemaName, table =>
        {
            table.HasCheckConstraint(
                "tags_key_format",
                "key ~ '^[a-z0-9][a-z0-9_.-]{0,63}$'");
            table.HasCheckConstraint(
                "tags_localization_key_group",
                "lower(localization_key) = lower('Tags.' || key)");
        });
        tag.HasOne<LocalizationEntryEntity>()
            .WithMany()
            .HasPrincipalKey(entity => entity.Key)
            .HasForeignKey(entity => entity.LocalizationKey)
            .OnDelete(DeleteBehavior.Cascade);

        var preference = modelBuilder.Entity<PlayerLanguagePreferenceEntity>();
        preference.HasOne<LocalizationLanguageEntity>()
            .WithMany()
            .HasPrincipalKey(entity => entity.Code)
            .HasForeignKey(entity => entity.LanguageCode)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
