using CustomEquipment.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class EquipmentShopMigrationTests
{
    private const string ShopMigration = "20260901143000_CreateEquipmentShops";
    private const string CleanupMigration = "20260901190000_RemoveAutomaticallySeededShopListings";

    [Fact]
    public void FreshDatabaseScript_DoesNotSeedShopListings()
    {
        var script = GenerateScript(toMigration: CleanupMigration);

        Assert.DoesNotContain("INSERT INTO custom_equipment.shop_listings", script);
    }

    [Fact]
    public void UpgradeScript_RemovesOnlyListingsCreatedWithTheirShopSettings()
    {
        var script = GenerateScript(ShopMigration, CleanupMigration);

        Assert.Contains("DELETE FROM custom_equipment.shop_listings", script);
        Assert.Contains("listing.shop_type = settings.shop_type", script);
        Assert.Contains("listing.created_at = settings.created_at", script);
    }

    private static string GenerateScript(
        string? fromMigration = null,
        string? toMigration = null
    )
    {
        var options = new DbContextOptionsBuilder<CustomEquipmentDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=custom_equipment_migration_test;Username=test;Password=test"
            )
            .Options;

        using var context = new CustomEquipmentDbContext(options);

        return context.GetService<IMigrator>().GenerateScript(fromMigration, toMigration);
    }
}
