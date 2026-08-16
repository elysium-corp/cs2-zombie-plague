using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CustomKnife.Database;

internal sealed class CustomKnifeDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CustomKnifeDbContext>
{
    public CustomKnifeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CUSTOM_KNIFE_DB_CONNECTION")
                               ?? "Host=127.0.0.1;Port=5432;Database=elysium_zp_server_1;Username=elysium_game";

        var options = new DbContextOptionsBuilder<CustomKnifeDbContext>();

        options.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    CustomKnifeDbContext.SchemaName
                );

                npgsql.CommandTimeout(5);
            }
        );

        return new CustomKnifeDbContext(options.Options);
    }
}