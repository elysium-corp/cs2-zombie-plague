using Microsoft.EntityFrameworkCore;

namespace Common.Database.Migrator;

public sealed class DatabaseMigrator<TContext>(IDbContextFactory<TContext> contextFactory) where TContext : DbContext
{
    public void Migrate()
    {
        using var context = contextFactory.CreateDbContext();

        context.Database.Migrate();
    }
}