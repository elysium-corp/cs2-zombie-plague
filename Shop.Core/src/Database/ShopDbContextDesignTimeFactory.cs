using Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shop.Core.Database;

internal sealed class ShopDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ShopDbContext>
{
    public ShopDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SHOP_DB_CONNECTION")
            ?? "Host=127.0.0.1;Port=5432;Database=elysium_zp_server_1;Username=postgres";
        var options = new DbContextOptionsBuilder<ShopDbContext>();
        options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
            DatabaseOptions.DefaultMigrationsHistoryTable,
            ShopDbContext.SchemaName));
        return new ShopDbContext(options.Options);
    }
}
