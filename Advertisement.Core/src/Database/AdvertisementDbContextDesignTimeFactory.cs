using Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Advertisement.Core.Database;

internal sealed class AdvertisementDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AdvertisementDbContext>
{
    public AdvertisementDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ADVERTISEMENT_DB_CONNECTION")
            ?? "Host=127.0.0.1;Port=5432;Database=elysium_zp_server_1;Username=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AdvertisementDbContext>();
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsHistoryTable(
                DatabaseOptions.DefaultMigrationsHistoryTable,
                AdvertisementDbContext.SchemaName);
        });

        return new AdvertisementDbContext(optionsBuilder.Options);
    }
}
