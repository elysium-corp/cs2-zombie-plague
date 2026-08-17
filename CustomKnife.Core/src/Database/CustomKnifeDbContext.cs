using CustomKnife.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomKnife.Database;

internal sealed class CustomKnifeDbContext(DbContextOptions<CustomKnifeDbContext> options) : DbContext(options)
{
    public const string SchemaName = "custom_knife";

    public DbSet<PlayerKnifeEntity> PlayerKnives => Set<PlayerKnifeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        base.OnModelCreating(modelBuilder);
    }
}