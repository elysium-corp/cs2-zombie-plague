using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZombiePlague.Core.Database.Entities;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Database.Configurations;

internal sealed class PlayerEntityConfiguration : IEntityTypeConfiguration<PlayerEntity>
{
    private const int ClassIdMaxLength = 64;

    public void Configure(EntityTypeBuilder<PlayerEntity> builder)
    {
        builder.ToTable("players");

        builder.HasKey(player => player.Id);

        builder.Property(player => player.Id)
            .HasColumnName("id");

        builder.Property(player => player.SteamId)
            .HasColumnName("steam_id");

        builder.Property(player => player.ZombieClassId)
            .HasColumnName("zombie_class")
            .HasMaxLength(ClassIdMaxLength)
            .HasDefaultValue(PlayerPreferences.DefaultZombieClassId)
            .IsRequired();

        builder.Property(player => player.HumanClassId)
            .HasColumnName("human_class")
            .HasMaxLength(ClassIdMaxLength)
            .HasDefaultValue(PlayerPreferences.DefaultHumanClassId)
            .IsRequired();

        builder.Property(player => player.UpdatedAtUtc)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(player => player.SteamId)
            .IsUnique()
            .HasDatabaseName("ux_players_steam_id");
    }
}
