using Microsoft.EntityFrameworkCore;
using Economy.Core.Database.Entities;

namespace Economy.Core.Database;

public sealed class EconomyDbContext(DbContextOptions<EconomyDbContext> options) : DbContext(options)
{
    internal DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("economy");
    }
}