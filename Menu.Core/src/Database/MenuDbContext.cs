using Menu.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Menu.Core.Database;

internal sealed class MenuDbContext(DbContextOptions<MenuDbContext> options) : DbContext(options)
{
    public const string SchemaName = "menu";

    internal DbSet<MenuProviderEntity> Providers => Set<MenuProviderEntity>();
    internal DbSet<MenuProviderInstanceEntity> ProviderInstances => Set<MenuProviderInstanceEntity>();
    internal DbSet<MenuProviderExportEntity> ProviderExports => Set<MenuProviderExportEntity>();
    internal DbSet<MenuDefinitionEntity> Definitions => Set<MenuDefinitionEntity>();
    internal DbSet<MenuDraftEntity> Drafts => Set<MenuDraftEntity>();
    internal DbSet<MenuRevisionEntity> Revisions => Set<MenuRevisionEntity>();
    internal DbSet<MenuReleaseEntity> Releases => Set<MenuReleaseEntity>();
    internal DbSet<MenuReleaseItemEntity> ReleaseItems => Set<MenuReleaseItemEntity>();
    internal DbSet<MenuReleaseTargetEntity> ReleaseTargets => Set<MenuReleaseTargetEntity>();
    internal DbSet<MenuReleaseHeadEntity> ReleaseHeads => Set<MenuReleaseHeadEntity>();
    internal DbSet<MenuCommandEntity> Commands => Set<MenuCommandEntity>();
    internal DbSet<MenuAuditLogEntity> AuditLog => Set<MenuAuditLogEntity>();
    internal DbSet<MenuServerStatusEntity> ServerStatuses => Set<MenuServerStatusEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MenuDbContext).Assembly);
    }
}
