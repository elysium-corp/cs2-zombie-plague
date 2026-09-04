using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.Core.Database;

namespace Shop.Core.Tests;

public sealed class ShopMigrationTests
{
    private const string FeatureMigration = "20260904160000_CreateShopModule";

    [Fact]
    public void FreshDatabaseScriptCreatesDirtyFallbackStateWithoutRevision()
    {
        var script = GenerateScript();

        Assert.Contains("CREATE TABLE IF NOT EXISTS shop.fallback_state", script);
        Assert.Contains("dirty BOOLEAN NOT NULL DEFAULT TRUE", script);
        Assert.DoesNotContain("revision", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("('human', 'Shop.Human.Title'", script);
        Assert.Contains("('zombie', 'Shop.Zombie.Title'", script);
    }

    [Fact]
    public void FreshDatabaseScriptMarksEveryShopCatalogTableDirty()
    {
        var script = GenerateScript();

        Assert.Contains("'shop.storefronts'", script);
        Assert.Contains("'shop.categories'", script);
        Assert.Contains("'shop.offers'", script);
        Assert.Contains("'shop.offer_privileges'", script);
        Assert.Contains("EXECUTE FUNCTION shop.mark_fallback_dirty()", script);
    }

    [Fact]
    public void FreshDatabaseScriptSupportsRootOffersAndPurchaseRules()
    {
        var script = GenerateScript();

        Assert.Contains("category_id BIGINT NULL", script);
        Assert.Contains("ammo_price INTEGER NULL", script);
        Assert.Contains("max_purchases_per_round INTEGER NOT NULL DEFAULT 0", script);
        Assert.Contains("max_purchases_per_map INTEGER NOT NULL DEFAULT 0", script);
        Assert.Contains("cooldown_seconds INTEGER NOT NULL DEFAULT 0", script);
        Assert.Contains("access_mode VARCHAR(16) NOT NULL DEFAULT 'everyone'", script);
        Assert.Contains("'priority', 'price', 'alphabetical'", script);
        Assert.Contains("^[A-Z0-9][A-Za-z0-9]*", script);
    }

    [Fact]
    public void FreshDatabaseScriptImportsLegacyEquipmentListingsOnce()
    {
        var script = GenerateScript();

        Assert.Contains("to_regclass('custom_equipment.shop_listings')", script);
        Assert.Contains("FROM custom_equipment.shop_listings listing", script);
        Assert.Contains("listing.item_internal_name = 'custom_equipment:armor'", script);
        Assert.Contains("THEN 'builtin' ELSE 'custom_equipment'", script);
    }

    private static string GenerateScript()
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql("Host=localhost;Database=shop_migration_test;Username=test;Password=test")
            .Options;
        using var context = new ShopDbContext(options);
        return context.GetService<IMigrator>().GenerateScript(toMigration: FeatureMigration);
    }
}
