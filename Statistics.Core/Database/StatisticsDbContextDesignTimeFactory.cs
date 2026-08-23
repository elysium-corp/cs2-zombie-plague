using Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Statistics.Core.Database;

internal sealed class StatisticsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<StatisticsDbContext>
{
    public StatisticsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("STATISTICS_DB_CONNECTION")
                               ?? "Host=127.0.0.1;Port=5432;Database=elysium_zp_server_1;Username=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<StatisticsDbContext>();

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsHistoryTable(
                DatabaseOptions.DefaultMigrationsHistoryTable,
                StatisticsDbContext.SchemaName
            );
        });

        return new StatisticsDbContext(optionsBuilder.Options);
    }
}

