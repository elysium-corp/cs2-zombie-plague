using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Economy.Core.Database.Migrations;

public sealed class EconomyDbContextDesignTimeFactory : IDesignTimeDbContextFactory<EconomyDbContext>
{
    public EconomyDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MONEY_SYSTEM_DB_CONNECTION")
                               ?? "Host=127.0.0.1;Port=5432;Database=elysium_zp_server_1;Username=elysium_game";

        var options = new DbContextOptionsBuilder<EconomyDbContext>()
            .UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(15);
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "economy");
                })
            .Options;

        return new EconomyDbContext(options);
    }
}