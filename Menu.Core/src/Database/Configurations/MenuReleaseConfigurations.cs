using Menu.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menu.Core.Database.Configurations;

internal sealed class MenuReleaseEntityConfiguration : IEntityTypeConfiguration<MenuReleaseEntity>
{
    public void Configure(EntityTypeBuilder<MenuReleaseEntity> builder)
    {
        builder.ToTable("releases", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_releases_number", "release_number > 0");
            table.HasCheckConstraint("ck_menu_releases_schema_version", "schema_version > 0");
            table.HasCheckConstraint("ck_menu_releases_api_version", "menu_core_api_version > 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_menu_releases");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ReleaseNumber).HasColumnName("release_number");
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version");
        builder.Property(x => x.MenuCoreApiVersion).HasColumnName("menu_core_api_version");
        builder.Property(x => x.RollbackOfReleaseId).HasColumnName("rollback_of_release_id");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(128);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.PublishedAt).HasColumnName("published_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.ReleaseNumber).IsUnique().HasDatabaseName("ux_menu_releases_number");
        builder.HasIndex(x => x.PublishedAt).IsDescending().HasDatabaseName("ix_menu_releases_published_at");
        builder.HasIndex(x => x.RollbackOfReleaseId).HasDatabaseName("ix_menu_releases_rollback_of");

        builder.HasOne(x => x.RollbackOfRelease)
            .WithMany(x => x.RollbackReleases)
            .HasForeignKey(x => x.RollbackOfReleaseId)
            .HasConstraintName("fk_menu_releases_rollback_of")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MenuReleaseItemEntityConfiguration : IEntityTypeConfiguration<MenuReleaseItemEntity>
{
    public void Configure(EntityTypeBuilder<MenuReleaseItemEntity> builder)
    {
        builder.ToTable("release_items", MenuDbContext.SchemaName);
        builder.HasKey(x => new { x.ReleaseId, x.DefinitionId }).HasName("pk_menu_release_items");
        builder.HasAlternateKey(x => new { x.ReleaseId, x.RevisionId })
            .HasName("ak_menu_release_items_release_revision");
        builder.Property(x => x.ReleaseId).HasColumnName("release_id");
        builder.Property(x => x.DefinitionId).HasColumnName("definition_id");
        builder.Property(x => x.RevisionId).HasColumnName("revision_id");

        builder.HasIndex(x => x.DefinitionId).HasDatabaseName("ix_menu_release_items_definition");
        builder.HasIndex(x => new { x.RevisionId, x.DefinitionId })
            .HasDatabaseName("ix_menu_release_items_revision");

        builder.HasOne(x => x.Release)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ReleaseId)
            .HasConstraintName("fk_menu_release_items_release")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Definition)
            .WithMany(x => x.ReleaseItems)
            .HasForeignKey(x => x.DefinitionId)
            .HasConstraintName("fk_menu_release_items_definition")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Revision)
            .WithMany(x => x.ReleaseItems)
            .HasForeignKey(x => new { x.RevisionId, x.DefinitionId })
            .HasPrincipalKey(x => new { x.Id, x.DefinitionId })
            .HasConstraintName("fk_menu_release_items_revision")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MenuReleaseTargetEntityConfiguration : IEntityTypeConfiguration<MenuReleaseTargetEntity>
{
    public void Configure(EntityTypeBuilder<MenuReleaseTargetEntity> builder)
    {
        builder.ToTable("release_targets", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_release_targets_server", "server_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$'");
            table.HasCheckConstraint(
                "ck_menu_release_targets_server_group",
                "server_group_key IS NULL OR server_group_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$'");
            table.HasCheckConstraint("ck_menu_release_targets_artifact", "jsonb_typeof(artifact_json::jsonb) = 'object'");
            table.HasCheckConstraint("ck_menu_release_targets_checksum", "checksum ~ '^[0-9a-f]{64}$'");
            table.HasCheckConstraint("ck_menu_release_targets_capabilities", "jsonb_typeof(capability_manifest) = 'object'");
        });

        builder.HasKey(x => new { x.ReleaseId, x.ServerKey }).HasName("pk_menu_release_targets");
        builder.Property(x => x.ReleaseId).HasColumnName("release_id");
        builder.Property(x => x.ServerKey).HasColumnName("server_key").HasMaxLength(64);
        builder.Property(x => x.ServerGroupKey).HasColumnName("server_group_key").HasMaxLength(64);
        builder.Property(x => x.ArtifactJson).HasColumnName("artifact_json").HasColumnType("text").IsRequired();
        builder.Property(x => x.Checksum).HasColumnName("checksum").HasColumnType("character(64)").IsRequired();
        builder.Property(x => x.CapabilityManifestJson)
            .HasColumnName("capability_manifest")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => new { x.ServerKey, x.ReleaseId })
            .HasDatabaseName("ix_menu_release_targets_server_release");
        builder.HasIndex(x => x.Checksum).HasDatabaseName("ix_menu_release_targets_checksum");

        builder.HasOne(x => x.Release)
            .WithMany(x => x.Targets)
            .HasForeignKey(x => x.ReleaseId)
            .HasConstraintName("fk_menu_release_targets_release")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MenuReleaseHeadEntityConfiguration : IEntityTypeConfiguration<MenuReleaseHeadEntity>
{
    public void Configure(EntityTypeBuilder<MenuReleaseHeadEntity> builder)
    {
        builder.ToTable("release_heads", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_release_heads_server", "server_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$'");
            table.HasCheckConstraint("ck_menu_release_heads_lock_version", "lock_version >= 0");
        });

        builder.HasKey(x => x.ServerKey).HasName("pk_menu_release_heads");
        builder.Property(x => x.ServerKey).HasColumnName("server_key").HasMaxLength(64);
        builder.Property(x => x.ReleaseId).HasColumnName("release_id");
        builder.Property(x => x.LockVersion)
            .HasColumnName("lock_version")
            .HasDefaultValue(0L)
            .IsConcurrencyToken();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(128);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => new { x.ReleaseId, x.ServerKey })
            .HasDatabaseName("ix_menu_release_heads_release");

        builder.HasOne(x => x.Target)
            .WithOne(x => x.Head)
            .HasForeignKey<MenuReleaseHeadEntity>(x => new { x.ReleaseId, x.ServerKey })
            .HasPrincipalKey<MenuReleaseTargetEntity>(x => new { x.ReleaseId, x.ServerKey })
            .HasConstraintName("fk_menu_release_heads_target")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MenuCommandEntityConfiguration : IEntityTypeConfiguration<MenuCommandEntity>
{
    public void Configure(EntityTypeBuilder<MenuCommandEntity> builder)
    {
        builder.ToTable("commands", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_commands_server", "server_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$'");
            table.HasCheckConstraint("ck_menu_commands_menu_key", "menu_key ~ '^[a-z0-9][a-z0-9_.-]{0,127}$'");
            table.HasCheckConstraint("ck_menu_commands_type", "command_type IN ('chat','console')");
            table.HasCheckConstraint("ck_menu_commands_alias", "btrim(alias) <> '' AND btrim(normalized_alias) <> ''");
            table.HasCheckConstraint(
                "ck_menu_commands_suppression",
                "suppression_mode IN ('none','on_match','on_success')");
        });

        builder.HasKey(x => x.Id).HasName("pk_menu_commands");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ReleaseId).HasColumnName("release_id");
        builder.Property(x => x.ServerKey).HasColumnName("server_key").HasMaxLength(64);
        builder.Property(x => x.RevisionId).HasColumnName("revision_id");
        builder.Property(x => x.MenuKey).HasColumnName("menu_key").HasMaxLength(128);
        builder.Property(x => x.CommandType).HasColumnName("command_type").HasMaxLength(16);
        builder.Property(x => x.Alias).HasColumnName("alias").HasMaxLength(128);
        builder.Property(x => x.NormalizedAlias).HasColumnName("normalized_alias").HasMaxLength(128);
        builder.Property(x => x.SuppressionMode)
            .HasColumnName("suppression_mode")
            .HasMaxLength(16)
            .HasDefaultValue(MenuDatabaseValues.SuppressionNone);
        builder.Property(x => x.Enabled).HasColumnName("enabled").HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => new { x.ReleaseId, x.ServerKey, x.CommandType, x.NormalizedAlias })
            .IsUnique()
            .HasFilter("enabled = TRUE")
            .HasDatabaseName("ux_menu_commands_active_route");
        builder.HasIndex(x => x.RevisionId).HasDatabaseName("ix_menu_commands_revision");
        builder.HasIndex(x => new { x.ReleaseId, x.ServerKey })
            .HasDatabaseName("ix_menu_commands_target");
        builder.HasIndex(x => new { x.ReleaseId, x.RevisionId })
            .HasDatabaseName("ix_menu_commands_release_item");
        builder.HasIndex(x => new { x.ServerKey, x.CommandType, x.NormalizedAlias })
            .HasFilter("enabled = TRUE")
            .HasDatabaseName("ix_menu_commands_server_lookup");

        builder.HasOne(x => x.ReleaseTarget)
            .WithMany(x => x.Commands)
            .HasForeignKey(x => new { x.ReleaseId, x.ServerKey })
            .HasConstraintName("fk_menu_commands_target")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReleaseItem)
            .WithMany(x => x.Commands)
            .HasForeignKey(x => new { x.ReleaseId, x.RevisionId })
            .HasPrincipalKey(x => new { x.ReleaseId, x.RevisionId })
            .HasConstraintName("fk_menu_commands_release_item")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
