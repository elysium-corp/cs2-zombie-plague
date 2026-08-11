using Microsoft.EntityFrameworkCore;
using Economy.Core.Database;

namespace Economy.Core.Initializer;

internal class EconomyDatabaseInitializer(IDbContextFactory<EconomyDbContext> contextFactory)
{
    public void Initialize()
    {
        using var context = contextFactory.CreateDbContext();

        context.Database.Migrate();
    }
}