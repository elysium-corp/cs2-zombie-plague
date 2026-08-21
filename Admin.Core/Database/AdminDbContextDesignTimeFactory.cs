using Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Admin.Core.Database;

internal sealed class AdminDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ADMIN_DB_CONNECTION")
                               ?? "Host=127.0.0.1;Port=5432;Database=elysium_zp_server_1;Username=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsHistoryTable(DatabaseOptions.DefaultMigrationsHistoryTable, AdminDbContext.SchemaName);
        });

        return new AdminDbContext(optionsBuilder.Options);
    }
}