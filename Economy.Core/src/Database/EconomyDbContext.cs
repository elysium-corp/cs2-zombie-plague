using Microsoft.EntityFrameworkCore;
using Economy.Core.Database.Entities;

namespace Economy.Core.Database;

public sealed class EconomyDbContext(DbContextOptions<EconomyDbContext> options) : DbContext(options)
{
    public const string SchemaName = "economy";
    
    internal DbSet<AccountEntity> Accounts => Set<AccountEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("economy");
    }
}