using Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Localization.Core.Database;

internal sealed class LocalizationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LocalizationDbContext>
{
    public LocalizationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LOCALIZATION_DB_CONNECTION")
            ?? "Host=127.0.0.1;Port=5432;Database=elysium_zp_server_1;Username=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<LocalizationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsHistoryTable(
                DatabaseOptions.DefaultMigrationsHistoryTable,
                LocalizationDbContext.SchemaName);
        });

        return new LocalizationDbContext(optionsBuilder.Options);
    }
}
