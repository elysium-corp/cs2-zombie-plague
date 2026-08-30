using Menu.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menu.Core.Database.Configurations;

internal sealed class MenuDefinitionEntityConfiguration : IEntityTypeConfiguration<MenuDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<MenuDefinitionEntity> builder)
    {
        builder.ToTable("definitions", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_definitions_key", "menu_key ~ '^[a-z0-9][a-z0-9_.-]{0,127}$'");
            table.HasCheckConstraint(
                "ck_menu_definitions_owner_provider",
                "owner_provider_key IS NULL OR owner_provider_key ~ '^[a-z0-9][a-z0-9_.-]{0,127}$'");
            table.HasCheckConstraint("ck_menu_definitions_status", "status IN ('draft','published','archived')");
        });

        builder.HasKey(x => x.Id).HasName("pk_menu_definitions");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.MenuKey).HasColumnName("menu_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.OwnerProviderKey).HasColumnName("owner_provider_key").HasMaxLength(128);
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(16)
            .HasDefaultValue(MenuDatabaseValues.DefinitionStatusDraft)
            .IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(128);
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(128);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.MenuKey).IsUnique().HasDatabaseName("ux_menu_definitions_key");
        builder.HasIndex(x => new { x.Status, x.UpdatedAt }).HasDatabaseName("ix_menu_definitions_status_updated");
    }
}

internal sealed class MenuDraftEntityConfiguration : IEntityTypeConfiguration<MenuDraftEntity>
{
    public void Configure(EntityTypeBuilder<MenuDraftEntity> builder)
    {
        builder.ToTable("drafts", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_drafts_schema_version", "schema_version > 0");
            table.HasCheckConstraint("ck_menu_drafts_payload", "jsonb_typeof(payload) = 'object'");
            table.HasCheckConstraint("ck_menu_drafts_lock_version", "lock_version >= 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_menu_drafts");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DefinitionId).HasColumnName("definition_id");
        builder.Property(x => x.BaseRevisionId).HasColumnName("base_revision_id");
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version");
        builder.Property(x => x.PayloadJson).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.LockVersion)
            .HasColumnName("lock_version")
            .HasDefaultValue(0L)
            .IsConcurrencyToken();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(128);
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(128);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.DefinitionId).IsUnique().HasDatabaseName("ux_menu_drafts_definition");
        builder.HasIndex(x => x.UpdatedAt).HasDatabaseName("ix_menu_drafts_updated_at");
        builder.HasIndex(x => new { x.BaseRevisionId, x.DefinitionId })
            .HasDatabaseName("ix_menu_drafts_base_revision");

        builder.HasOne(x => x.Definition)
            .WithOne(x => x.Draft)
            .HasForeignKey<MenuDraftEntity>(x => x.DefinitionId)
            .HasConstraintName("fk_menu_drafts_definition")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.BaseRevision)
            .WithMany()
            .HasForeignKey(x => new { x.BaseRevisionId, x.DefinitionId })
            .HasPrincipalKey(x => new { x.Id, x.DefinitionId })
            .HasConstraintName("fk_menu_drafts_base_revision")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MenuRevisionEntityConfiguration : IEntityTypeConfiguration<MenuRevisionEntity>
{
    public void Configure(EntityTypeBuilder<MenuRevisionEntity> builder)
    {
        builder.ToTable("revisions", MenuDbContext.SchemaName, table =>
        {
            table.HasCheckConstraint("ck_menu_revisions_number", "revision_number > 0");
            table.HasCheckConstraint("ck_menu_revisions_schema_version", "schema_version > 0");
            table.HasCheckConstraint("ck_menu_revisions_payload", "jsonb_typeof(payload) = 'object'");
            table.HasCheckConstraint("ck_menu_revisions_checksum", "checksum ~ '^[0-9a-f]{64}$'");
        });

        builder.HasKey(x => x.Id).HasName("pk_menu_revisions");
        builder.HasAlternateKey(x => new { x.Id, x.DefinitionId })
            .HasName("ak_menu_revisions_id_definition");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DefinitionId).HasColumnName("definition_id");
        builder.Property(x => x.RevisionNumber).HasColumnName("revision_number");
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version");
        builder.Property(x => x.PayloadJson).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Checksum).HasColumnName("checksum").HasColumnType("character(64)").IsRequired();
        builder.Property(x => x.BasedOnRevisionId).HasColumnName("based_on_revision_id");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(128);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.PublishedAt).HasColumnName("published_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => new { x.DefinitionId, x.RevisionNumber })
            .IsUnique()
            .HasDatabaseName("ux_menu_revisions_definition_number");
        builder.HasIndex(x => new { x.DefinitionId, x.PublishedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_menu_revisions_definition_published");
        builder.HasIndex(x => new { x.BasedOnRevisionId, x.DefinitionId })
            .HasDatabaseName("ix_menu_revisions_based_on");

        builder.HasOne(x => x.Definition)
            .WithMany(x => x.Revisions)
            .HasForeignKey(x => x.DefinitionId)
            .HasConstraintName("fk_menu_revisions_definition")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BasedOnRevision)
            .WithMany(x => x.DerivedRevisions)
            .HasForeignKey(x => new { x.BasedOnRevisionId, x.DefinitionId })
            .HasPrincipalKey(x => new { x.Id, x.DefinitionId })
            .HasConstraintName("fk_menu_revisions_based_on")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
