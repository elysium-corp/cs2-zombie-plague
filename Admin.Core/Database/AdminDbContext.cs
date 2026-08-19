using Admin.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Database;

public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public const string SchemaName = "admin";

    internal DbSet<PlayerPrivilegeEntity> PlayerPrivileges => Set<PlayerPrivilegeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
    }
}