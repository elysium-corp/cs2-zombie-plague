using Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Menu.Core.Database;

internal sealed class MenuDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MenuDbContext>
{
    public MenuDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MENU_DB_CONNECTION")
            ?? "Host=127.0.0.1;Port=5432;Database=elysium_zp_server_1;Username=postgres";

        var builder = new DbContextOptionsBuilder<MenuDbContext>();
        builder.UseNpgsql(connectionString, options =>
            options.MigrationsHistoryTable(
                DatabaseOptions.DefaultMigrationsHistoryTable,
                MenuDbContext.SchemaName));

        return new MenuDbContext(builder.Options);
    }
}
