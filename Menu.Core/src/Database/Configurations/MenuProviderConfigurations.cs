using Menu.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menu.Core.Database.Configurations;

internal sealed class MenuProviderEntityConfiguration : IEntityTypeConfiguration<MenuProviderEntity>
{
    public void Configure(EntityTypeBuilder<MenuProviderEntity> builder)
    {
        builder.ToTable("providers", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_providers_key", "provider_key ~ '^[a-z0-9][a-z0-9_.-]{0,127}$'");
            table.HasCheckConstraint("ck_menu_providers_metadata", "jsonb_typeof(metadata) = 'object'");
        });

        builder.HasKey(x => x.Id).HasName("pk_menu_providers");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProviderKey).HasColumnName("provider_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(128).IsRequired();
        builder.Property(x => x.MetadataJson)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.ProviderKey).IsUnique().HasDatabaseName("ux_menu_providers_key");
        builder.HasIndex(x => x.UpdatedAt).HasDatabaseName("ix_menu_providers_updated_at");
    }
}

internal sealed class MenuProviderInstanceEntityConfiguration : IEntityTypeConfiguration<MenuProviderInstanceEntity>
{
    public void Configure(EntityTypeBuilder<MenuProviderInstanceEntity> builder)
    {
        builder.ToTable("provider_instances", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_provider_instances_server_key", "server_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$'");
            table.HasCheckConstraint(
                "ck_menu_provider_instances_status",
                "status IN ('online','offline','incompatible','api_outdated','error')");
            table.HasCheckConstraint("ck_menu_provider_instances_api_version", "menu_api_version > 0");
            table.HasCheckConstraint("ck_menu_provider_instances_generation", "generation > 0");
            table.HasCheckConstraint("ck_menu_provider_instances_capabilities", "jsonb_typeof(capabilities) = 'array'");
            table.HasCheckConstraint("ck_menu_provider_instances_metadata", "jsonb_typeof(metadata) = 'object'");
        });

        builder.HasKey(x => x.Id).HasName("pk_menu_provider_instances");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProviderId).HasColumnName("provider_id");
        builder.Property(x => x.ServerKey).HasColumnName("server_key").HasMaxLength(64).IsRequired();
        builder.Property(x => x.PluginVersion).HasColumnName("plugin_version").HasMaxLength(32).IsRequired();
        builder.Property(x => x.MenuApiVersion).HasColumnName("menu_api_version");
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasDefaultValue(MenuDatabaseValues.ProviderStatusOffline)
            .IsRequired();
        builder.Property(x => x.CapabilitiesJson)
            .HasColumnName("capabilities")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();
        builder.Property(x => x.MetadataJson)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.Generation).HasColumnName("generation").HasDefaultValue(1L);
        builder.Property(x => x.RegisteredAt).HasColumnName("registered_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.OfflineAt).HasColumnName("offline_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.LastError).HasColumnName("last_error");

        builder.HasIndex(x => new { x.ProviderId, x.ServerKey })
            .IsUnique()
            .HasDatabaseName("ux_menu_provider_instances_provider_server");
        builder.HasIndex(x => x.SessionId).HasDatabaseName("ix_menu_provider_instances_session");
        builder.HasIndex(x => new { x.ServerKey, x.Status, x.LastSeenAt })
            .HasDatabaseName("ix_menu_provider_instances_server_status_seen");

        builder.HasOne(x => x.Provider)
            .WithMany(x => x.Instances)
            .HasForeignKey(x => x.ProviderId)
            .HasConstraintName("fk_menu_provider_instances_provider")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MenuProviderExportEntityConfiguration : IEntityTypeConfiguration<MenuProviderExportEntity>
{
    public void Configure(EntityTypeBuilder<MenuProviderExportEntity> builder)
    {
        builder.ToTable("provider_exports", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_provider_exports_type", "export_type IN ('menu','action')");
            table.HasCheckConstraint("ck_menu_provider_exports_key", "export_key ~ '^[a-z0-9][a-z0-9_.-]{0,127}$'");
            table.HasCheckConstraint("ck_menu_provider_exports_generation", "declared_generation > 0");
            table.HasCheckConstraint(
                "ck_menu_provider_exports_schema",
                "schema IS NULL OR jsonb_typeof(schema) = 'object'");
            table.HasCheckConstraint("ck_menu_provider_exports_metadata", "jsonb_typeof(metadata) = 'object'");
        });

        builder.HasKey(x => x.Id).HasName("pk_menu_provider_exports");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProviderInstanceId).HasColumnName("provider_instance_id");
        builder.Property(x => x.ExportType).HasColumnName("export_type").HasMaxLength(16).IsRequired();
        builder.Property(x => x.ExportKey).HasColumnName("export_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(128).IsRequired();
        builder.Property(x => x.SchemaJson).HasColumnName("schema").HasColumnType("jsonb");
        builder.Property(x => x.MetadataJson)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        builder.Property(x => x.IsDeclared).HasColumnName("is_declared").HasDefaultValue(true);
        builder.Property(x => x.DeclaredGeneration).HasColumnName("declared_generation");
        builder.Property(x => x.FirstSeenAt).HasColumnName("first_seen_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => new { x.ProviderInstanceId, x.ExportType, x.ExportKey })
            .IsUnique()
            .HasDatabaseName("ux_menu_provider_exports_instance_type_key");
        builder.HasIndex(x => new { x.ProviderInstanceId, x.IsDeclared })
            .HasDatabaseName("ix_menu_provider_exports_declared");

        builder.HasOne(x => x.ProviderInstance)
            .WithMany(x => x.Exports)
            .HasForeignKey(x => x.ProviderInstanceId)
            .HasConstraintName("fk_menu_provider_exports_instance")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
