using Microsoft.EntityFrameworkCore;
using ZombiePlague.Core.Database.Entities;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Database;

public sealed class ZombiePlagueDbContext(DbContextOptions<ZombiePlagueDbContext> options) : DbContext(options)
{
    public const string SchemaName = "zombie_plague";

    internal DbSet<PlayerEntity> Players => Set<PlayerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
        
        var player = modelBuilder.Entity<PlayerEntity>();
        player.Property(entity => entity.ZombieClassId).HasDefaultValue(PlayerPreferences.DefaultZombieClassId);
        player.Property(entity => entity.HumanClassId).HasDefaultValue(PlayerPreferences.DefaultHumanClassId);
        player.Property(entity => entity.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
