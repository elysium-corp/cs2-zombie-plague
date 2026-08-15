using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ZombiePlague.Core.Database;

namespace ZombiePlague.Core.Database.Migrations;

public sealed class ZombiePlagueDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<ZombiePlagueDbContext>
{
    private const string ConnectionVariableName = "ZOMBIE_PLAGUE_DB_CONNECTION";

    public ZombiePlagueDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariableName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Environment variable '{ConnectionVariableName}' is required for EF Core migrations."
            );
        }

        var options = new DbContextOptionsBuilder<ZombiePlagueDbContext>()
            .UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(15);
                    npgsqlOptions.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        ZombiePlagueDbContext.SchemaName
                    );
                }
            )
            .Options;

        return new ZombiePlagueDbContext(options);
    }
}
