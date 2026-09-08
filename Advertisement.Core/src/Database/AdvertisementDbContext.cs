using Advertisement.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Advertisement.Core.Database;

internal sealed class AdvertisementDbContext(DbContextOptions<AdvertisementDbContext> options) : DbContext(options)
{
    public const string SchemaName = "advertisement";
    public const string CoreSchemaName = "core";

    internal DbSet<AdvertisementSettingsEntity> Settings => Set<AdvertisementSettingsEntity>();
    internal DbSet<AdvertisementMessageEntity> Messages => Set<AdvertisementMessageEntity>();
    internal DbSet<AdvertisementMessageTranslationEntity> MessageTranslations => Set<AdvertisementMessageTranslationEntity>();
    internal DbSet<PlayerPreferenceEntity> PlayerPreferences => Set<PlayerPreferenceEntity>();

    private const string PostgreSqlCurrentTimestamp = "CURRENT_TIMESTAMP";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        ConfigureSettings(modelBuilder);
        ConfigureMessages(modelBuilder);
        modelBuilder.Entity<BannerTemplateEntity>().Property(x => x.UpdatedAt).HasDefaultValueSql(PostgreSqlCurrentTimestamp);
        modelBuilder.Entity<AdvertisementMessageEntity>().HasOne(x => x.BannerTemplate).WithMany()
            .HasForeignKey(x => x.BannerTemplateKey).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AdvertisementMessageEntity>().Property(x => x.BannerParametersJson).HasDefaultValueSql("'{}'::jsonb");
        ConfigureMessageTranslations(modelBuilder);
        ConfigurePlayerPreferences(modelBuilder);
    }

    private static void ConfigureSettings(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AdvertisementSettingsEntity>();

        entity.Property(x => x.Enabled).HasDefaultValue(true);
        entity.Property(x => x.DefaultLocale).HasDefaultValue("ru");
        entity.Property(x => x.AllowedLocalesJson)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[\"ru\",\"en\",\"uk\",\"pl\",\"de\"]'::jsonb");
        entity.Property(x => x.IntervalSeconds).HasDefaultValue(90);
        entity.Property(x => x.RefreshIntervalSeconds).HasDefaultValue(30);
        entity.Property(x => x.InitialDelaySeconds).HasDefaultValue(45);
        entity.Property(x => x.OrderMode).HasDefaultValue("sequential");
        entity.Property(x => x.ExcludeBotsFromPlayers).HasDefaultValue(true);
        entity.Property(x => x.ConfigurationVersion).HasDefaultValue(1L);
        entity.Property(x => x.FallbackExportedVersion).HasDefaultValue(0L);
        entity.Property(x => x.CreatedAt).HasDefaultValueSql(PostgreSqlCurrentTimestamp);
        entity.Property(x => x.UpdatedAt).HasDefaultValueSql(PostgreSqlCurrentTimestamp);

        entity.ToTable(
            "settings",
            SchemaName,
            table =>
            {
                table.HasCheckConstraint("ck_advertisement_settings_interval", "interval_seconds >= 10");
                table.HasCheckConstraint("ck_advertisement_settings_refresh_interval", "refresh_interval_seconds >= 5");
                table.HasCheckConstraint("ck_advertisement_settings_initial_delay", "initial_delay_seconds >= 0");
                table.HasCheckConstraint(
                    "ck_advertisement_settings_order_mode",
                    "order_mode IN ('sequential', 'random', 'weighted_random')");
                table.HasCheckConstraint(
                    "ck_advertisement_settings_fallback_exported_version",
                    "fallback_exported_version >= 0");
            });
    }

    private static void ConfigureMessages(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AdvertisementMessageEntity>();

        entity.Property(x => x.Type).HasDefaultValue("information");
        entity.Property(x => x.DisplayType).HasDefaultValue("chat");
        entity.Property(x => x.HudPosition).HasDefaultValue("bottom_left");
        entity.Property(x => x.HudDurationSeconds).HasDefaultValue(8d);
        entity.Property(x => x.HudStyle).HasDefaultValue("notice");
        entity.Property(x => x.Enabled).HasDefaultValue(true);
        entity.Property(x => x.Priority).HasDefaultValue(0);
        entity.Property(x => x.Weight).HasDefaultValue(100);
        entity.Property(x => x.SortOrder).HasDefaultValue(0);
        entity.Property(x => x.DispatchMode).HasDefaultValue("periodic");
        entity.Property(x => x.DailyTimesJson)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");
        entity.Property(x => x.AudienceType).HasDefaultValue("all");
        entity.Property(x => x.CreatedAt).HasDefaultValueSql(PostgreSqlCurrentTimestamp);
        entity.Property(x => x.UpdatedAt).HasDefaultValueSql(PostgreSqlCurrentTimestamp);

        entity.ToTable(
            "messages",
            SchemaName,
            table =>
            {
                table.HasCheckConstraint(
                    "ck_advertisement_messages_type",
                    "type IN ('information', 'advertisement', 'tip', 'warning', 'event', 'system')");
                table.HasCheckConstraint("ck_advertisement_messages_display_type", "display_type IN ('chat', 'hud', 'chat_and_hud')");
                table.HasCheckConstraint("messages_hud_position_valid",
                    "hud_position IN ('top_left', 'top_center', 'top_right', 'middle_left', 'center', 'middle_right', 'bottom_left', 'bottom_center', 'bottom_right')");
                table.HasCheckConstraint("messages_hud_duration_valid", "hud_duration_seconds BETWEEN 0.5 AND 60");
                table.HasCheckConstraint("messages_hud_style_valid", "hud_style IN ('notice', 'banner')");
                table.HasCheckConstraint("messages_hud_key_required", "display_type = 'chat' OR (hud_localization_key IS NOT NULL AND btrim(hud_localization_key) <> '')");
                table.HasCheckConstraint("ck_advertisement_messages_weight", "weight >= 0");
                table.HasCheckConstraint(
                    "ck_advertisement_messages_interval",
                    "interval_seconds IS NULL OR interval_seconds >= 10");
                table.HasCheckConstraint(
                    "messages_dispatch_mode_valid",
                    "dispatch_mode IN ('periodic', 'daily', 'manual')");
                table.HasCheckConstraint(
                    "messages_daily_times_array",
                    "jsonb_typeof(daily_times) = 'array'");
                table.HasCheckConstraint(
                    "messages_audience_valid",
                    "(audience_type = 'all' AND audience_group IS NULL) OR " +
                    "(audience_type = 'admin_group' AND audience_group IS NOT NULL AND btrim(audience_group) <> '')");
                table.HasCheckConstraint(
                    "ck_advertisement_messages_min_players",
                    "min_players IS NULL OR min_players >= 0");
                table.HasCheckConstraint(
                    "ck_advertisement_messages_max_players",
                    "max_players IS NULL OR max_players >= 0");
                table.HasCheckConstraint(
                    "ck_advertisement_messages_player_range",
                    "min_players IS NULL OR max_players IS NULL OR min_players <= max_players");
                table.HasCheckConstraint(
                    "ck_advertisement_messages_time_range",
                    "starts_at IS NULL OR ends_at IS NULL OR starts_at < ends_at");
                table.HasCheckConstraint(
                    "ck_advertisement_messages_key_format",
                    @"key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$'");
            });

        entity.HasIndex(x => x.Key)
            .HasDatabaseName("messages_key_unique")
            .IsUnique();

        entity.HasIndex(x => x.LocalizationKey)
            .HasDatabaseName("messages_localization_key_idx");

        entity.HasIndex(x => x.HudLocalizationKey)
            .HasDatabaseName("messages_hud_localization_key_idx")
            .HasFilter("hud_localization_key IS NOT NULL");

        entity.HasIndex(x => x.TagKey)
            .HasDatabaseName("messages_tag_key_idx")
            .HasFilter("tag_key IS NOT NULL");

        entity.HasIndex(x => new { x.Enabled, x.Priority, x.SortOrder, x.Id })
            .HasDatabaseName("messages_active_idx")
            .IsDescending(false, true, false, false);

        entity.HasIndex(x => new { x.StartsAt, x.EndsAt })
            .HasDatabaseName("messages_schedule_idx")
            .HasFilter("enabled = TRUE");

        entity.HasIndex(x => new { x.Enabled, x.DispatchMode })
            .HasDatabaseName("messages_dispatch_idx");

    }

    private static void ConfigureMessageTranslations(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AdvertisementMessageTranslationEntity>();
        entity.HasKey(x => new { x.MessageId, x.Locale });

        entity.Property(x => x.CreatedAt).HasDefaultValueSql(PostgreSqlCurrentTimestamp);
        entity.Property(x => x.UpdatedAt).HasDefaultValueSql(PostgreSqlCurrentTimestamp);

        entity.HasOne(x => x.Message)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurePlayerPreferences(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PlayerPreferenceEntity>();
        entity.Property(x => x.UpdatedAt).HasDefaultValueSql(PostgreSqlCurrentTimestamp);
    }
}
