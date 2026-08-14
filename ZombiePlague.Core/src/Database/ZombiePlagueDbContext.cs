using Microsoft.EntityFrameworkCore;
using ZombiePlague.Core.Database.Entities;

namespace ZombiePlague.Core.Database;

public sealed class ZombiePlagueDbContext(DbContextOptions<ZombiePlagueDbContext> options)
    : DbContext(options)
{
    public const string SchemaName = "zombie_plague";

    internal DbSet<PlayerEntity> Players => Set<PlayerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZombiePlagueDbContext).Assembly);
    }
}
