using Menu.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menu.Core.Database.Configurations;

internal sealed class MenuAuditLogEntityConfiguration : IEntityTypeConfiguration<MenuAuditLogEntity>
{
    public void Configure(EntityTypeBuilder<MenuAuditLogEntity> builder)
    {
        builder.ToTable("audit_log", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_audit_action", "btrim(action) <> ''");
            table.HasCheckConstraint("ck_menu_audit_entity_type", "btrim(entity_type) <> ''");
            table.HasCheckConstraint("ck_menu_audit_changes", "jsonb_typeof(changes) = 'object'");
            table.HasCheckConstraint("ck_menu_audit_metadata", "jsonb_typeof(metadata) = 'object'");
        });

        builder.HasKey(x => x.Id).HasName("pk_menu_audit_log");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ActorId).HasColumnName("actor_id").HasMaxLength(128);
        builder.Property(x => x.ActorDisplayName).HasColumnName("actor_display_name").HasMaxLength(128);
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.EntityKey).HasColumnName("entity_key").HasMaxLength(128);
        builder.Property(x => x.ServerKey).HasColumnName("server_key").HasMaxLength(64);
        builder.Property(x => x.ReleaseId).HasColumnName("release_id");
        builder.Property(x => x.RevisionId).HasColumnName("revision_id");
        builder.Property(x => x.ChangesJson)
            .HasColumnName("changes")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        builder.Property(x => x.MetadataJson)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.CreatedAt).IsDescending().HasDatabaseName("ix_menu_audit_created_at");
        builder.HasIndex(x => new { x.EntityType, x.EntityKey, x.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_menu_audit_entity_created");
        builder.HasIndex(x => new { x.ActorId, x.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_menu_audit_actor_created");
        builder.HasIndex(x => x.ReleaseId).HasDatabaseName("ix_menu_audit_release");
        builder.HasIndex(x => x.RevisionId).HasDatabaseName("ix_menu_audit_revision");

        builder.HasOne(x => x.Release)
            .WithMany()
            .HasForeignKey(x => x.ReleaseId)
            .HasConstraintName("fk_menu_audit_release")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Revision)
            .WithMany()
            .HasForeignKey(x => x.RevisionId)
            .HasConstraintName("fk_menu_audit_revision")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MenuServerStatusEntityConfiguration : IEntityTypeConfiguration<MenuServerStatusEntity>
{
    public void Configure(EntityTypeBuilder<MenuServerStatusEntity> builder)
    {
        builder.ToTable("server_status", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_server_status_key", "server_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$'");
            table.HasCheckConstraint("ck_menu_server_status_generation", "generation > 0");
            table.HasCheckConstraint("ck_menu_server_status_menu_api", "menu_api_version > 0");
            table.HasCheckConstraint("ck_menu_server_status_schema", "schema_version > 0");
            table.HasCheckConstraint("ck_menu_server_status_capabilities", "jsonb_typeof(capabilities) = 'object'");
            table.HasCheckConstraint(
                "ck_menu_server_status_source",
                "loaded_source IS NULL OR loaded_source IN ('database','lkg','fallback')");
            table.HasCheckConstraint(
                "ck_menu_server_status_validation",
                "validation_status IN ('not_loaded','valid','invalid','degraded')");
            table.HasCheckConstraint(
                "ck_menu_server_status_active_checksum",
                "active_checksum IS NULL OR active_checksum ~ '^[0-9a-f]{64}$'");
        });

        builder.HasKey(x => x.ServerKey).HasName("pk_menu_server_status");
        builder.Property(x => x.ServerKey).HasColumnName("server_key").HasMaxLength(64);
        builder.Property(x => x.RuntimeSessionId).HasColumnName("runtime_session_id");
        builder.Property(x => x.Generation).HasColumnName("generation").HasDefaultValue(1L);
        builder.Property(x => x.MenuCoreVersion).HasColumnName("menu_core_version").HasMaxLength(32);
        builder.Property(x => x.SwiftlyVersion).HasColumnName("swiftly_version").HasMaxLength(64);
        builder.Property(x => x.MenuApiVersion).HasColumnName("menu_api_version");
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version");
        builder.Property(x => x.CapabilitiesJson)
            .HasColumnName("capabilities")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        builder.Property(x => x.ActiveReleaseId).HasColumnName("active_release_id");
        builder.Property(x => x.ActiveChecksum).HasColumnName("active_checksum").HasColumnType("character(64)");
        builder.Property(x => x.LoadedSource).HasColumnName("loaded_source").HasMaxLength(32);
        builder.Property(x => x.LastDbSyncAt).HasColumnName("last_db_sync_at");
        builder.Property(x => x.LastKnownGoodReleaseId).HasColumnName("lkg_release_id");
        builder.Property(x => x.FallbackReleaseId).HasColumnName("fallback_release_id");
        builder.Property(x => x.ValidationStatus)
            .HasColumnName("validation_status")
            .HasMaxLength(32)
            .HasDefaultValue(MenuDatabaseValues.ValidationNotLoaded);
        builder.Property(x => x.LastError).HasColumnName("last_error");
        builder.Property(x => x.HeartbeatAt).HasColumnName("heartbeat_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.RuntimeSessionId).HasDatabaseName("ix_menu_server_status_session");
        builder.HasIndex(x => x.HeartbeatAt).HasDatabaseName("ix_menu_server_status_heartbeat");
    }
}
