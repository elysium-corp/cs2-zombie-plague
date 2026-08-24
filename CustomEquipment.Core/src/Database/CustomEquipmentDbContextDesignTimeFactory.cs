using Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CustomEquipment.Database;

internal sealed class CustomEquipmentDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CustomEquipmentDbContext>
{
    public CustomEquipmentDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CUSTOM_EQUIPMENT_DB_CONNECTION")
                               ?? "Host=127.0.0.1;Port=5432;Database=elysium_zp_server_1;Username=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<CustomEquipmentDbContext>();

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsHistoryTable(
                DatabaseOptions.DefaultMigrationsHistoryTable,
                CustomEquipmentDbContext.SchemaName
            );
        });

        return new CustomEquipmentDbContext(optionsBuilder.Options);
    }
}
