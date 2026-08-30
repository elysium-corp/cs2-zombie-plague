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
    internal DbSet<PlayerLanguagePreferenceEntity> PlayerPreferences => Set<PlayerLanguagePreferenceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);

        var language = modelBuilder.Entity<LocalizationLanguageEntity>();
        language.Property(entity => entity.Enabled).HasDefaultValue(true);
        language.Property(entity => entity.SortOrder).HasDefaultValue(0);

        var settings = modelBuilder.Entity<LocalizationSettingsEntity>();
        settings.Property(entity => entity.Id).HasDefaultValue((short)1);
        settings.Property(entity => entity.ServerFallbackLanguage).HasDefaultValue("ru");
        settings.Property(entity => entity.RefreshIntervalSeconds).HasDefaultValue(30);
        settings.Property(entity => entity.LocalCacheEnabled).HasDefaultValue(true);
        settings.Property(entity => entity.LogMissingKeys).HasDefaultValue(true);
        settings.Property(entity => entity.ConfigurationVersion).HasDefaultValue(1L);
        settings.ToTable("settings", SchemaName, table =>
            table.HasCheckConstraint("settings_singleton", "id = 1"));

        var entry = modelBuilder.Entity<LocalizationEntryEntity>();
        entry.Property(entity => entity.IsCritical).HasDefaultValue(false);

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
            .OnDelete(DeleteBehavior.Restrict);

        var preference = modelBuilder.Entity<PlayerLanguagePreferenceEntity>();
        preference.HasOne<LocalizationLanguageEntity>()
            .WithMany()
            .HasPrincipalKey(entity => entity.Code)
            .HasForeignKey(entity => entity.LanguageCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
