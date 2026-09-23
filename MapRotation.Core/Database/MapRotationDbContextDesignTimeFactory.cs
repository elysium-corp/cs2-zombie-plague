using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MapRotation.Core.Database;

internal sealed class MapRotationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MapRotationDbContext>
{
    public MapRotationDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MapRotationDbContext>()
        .UseNpgsql("Host=localhost;Database=map_rotation_design;Username=postgres;Password=design-only",
            options => options.MigrationsHistoryTable("__EFMigrationsHistory", MapRotationDbContext.SchemaName)).Options);
}
