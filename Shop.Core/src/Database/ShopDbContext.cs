using Microsoft.EntityFrameworkCore;
using Shop.Core.Database.Entities;

namespace Shop.Core.Database;

internal sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public const string SchemaName = "shop";

    internal DbSet<ShopStorefrontEntity> Storefronts => Set<ShopStorefrontEntity>();
    internal DbSet<ShopCategoryEntity> Categories => Set<ShopCategoryEntity>();
    internal DbSet<ShopOfferEntity> Offers => Set<ShopOfferEntity>();
    internal DbSet<ShopOfferPrivilegeEntity> OfferPrivileges => Set<ShopOfferPrivilegeEntity>();
    internal DbSet<ShopFallbackStateEntity> FallbackState => Set<ShopFallbackStateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<ShopStorefrontEntity>(entity =>
        {
            entity.Property(item => item.Enabled).HasDefaultValue(true);
            entity.Property(item => item.SortMode).HasDefaultValue("priority");
            entity.ToTable(
                "storefronts",
                SchemaName,
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_shop_storefront_type",
                        "shop_type IN ('human', 'zombie')");
                    table.HasCheckConstraint(
                        "ck_shop_storefront_sort",
                        "sort_mode IN ('priority', 'price', 'alphabetical')");
                    table.HasCheckConstraint(
                        "ck_shop_storefront_title_key",
                        "title_key ~ '^[A-Z0-9][A-Za-z0-9]*(\\.[A-Z0-9][A-Za-z0-9]*)*$'");
                });
        });

        modelBuilder.Entity<ShopCategoryEntity>(entity =>
        {
            entity.HasIndex(item => new { item.ShopType, item.Key }).IsUnique();
            entity.HasIndex(item => new { item.ShopType, item.Enabled, item.SortOrder });
            entity.Property(item => item.Enabled).HasDefaultValue(true);
            entity.Property(item => item.SortOrder).HasDefaultValue(0);
            entity.HasOne<ShopStorefrontEntity>()
                .WithMany()
                .HasForeignKey(item => item.ShopType)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(
                "categories",
                SchemaName,
                table => table.HasCheckConstraint(
                    "ck_shop_category_localization_keys",
                    "display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\\.[A-Z0-9][A-Za-z0-9]*)*$' " +
                    "AND (description_key IS NULL OR description_key ~ " +
                    "'^[A-Z0-9][A-Za-z0-9]*(\\.[A-Z0-9][A-Za-z0-9]*)*$')"));
        });

        modelBuilder.Entity<ShopOfferEntity>(entity =>
        {
            entity.HasIndex(item => new { item.ShopType, item.ProviderKey, item.ItemKey }).IsUnique();
            entity.HasIndex(item => new { item.ShopType, item.Enabled, item.SortOrder });
            entity.Property(item => item.AmmoAmount).HasDefaultValue(1);
            entity.Property(item => item.AccessMode).HasDefaultValue("everyone");
            entity.Property(item => item.Enabled).HasDefaultValue(true);
            entity.Property(item => item.SettingsJson).HasDefaultValueSql("'{}'::jsonb");
            entity.HasOne<ShopStorefrontEntity>()
                .WithMany()
                .HasForeignKey(item => item.ShopType)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Category)
                .WithMany(item => item.Offers)
                .HasForeignKey(item => item.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.ToTable(
                "offers",
                SchemaName,
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_shop_offer_access",
                        "access_mode IN ('everyone', 'any', 'all')");
                    table.HasCheckConstraint(
                        "ck_shop_offer_values",
                        "price >= 0 AND (ammo_price IS NULL OR ammo_price >= 0) AND ammo_amount > 0 " +
                        "AND max_purchases_per_round >= 0 AND max_purchases_per_map >= 0 " +
                        "AND cooldown_seconds >= 0");
                    table.HasCheckConstraint(
                        "ck_shop_offer_localization_keys",
                        "display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\\.[A-Z0-9][A-Za-z0-9]*)*$' " +
                        "AND (description_key IS NULL OR description_key ~ " +
                        "'^[A-Z0-9][A-Za-z0-9]*(\\.[A-Z0-9][A-Za-z0-9]*)*$')");
                });
        });

        modelBuilder.Entity<ShopOfferPrivilegeEntity>(entity =>
        {
            entity.HasKey(item => new { item.OfferId, item.PrivilegeKey });
            entity.HasOne(item => item.Offer)
                .WithMany(item => item.Privileges)
                .HasForeignKey(item => item.OfferId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShopFallbackStateEntity>(entity =>
        {
            entity.Property(item => item.Dirty).HasDefaultValue(true);
            entity.ToTable(
                "fallback_state",
                SchemaName,
                table => table.HasCheckConstraint("ck_shop_fallback_singleton", "id = 1"));
        });
    }
}
