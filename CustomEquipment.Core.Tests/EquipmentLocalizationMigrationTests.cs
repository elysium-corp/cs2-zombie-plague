using CustomEquipment.Database;
using CustomEquipment.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class EquipmentLocalizationMigrationTests
{
    private const string PreviousMigration =
        "20260901190000_RemoveAutomaticallySeededShopListings";
    private const string FeatureMigration =
        "20260901220000_AddLocalizationKeysAndImages";

    [Fact]
    public void UpgradeScript_BackfillsKeysBeforeMakingThemRequired()
    {
        var script = GenerateScript(PreviousMigration, FeatureMigration);

        AssertAppearsBefore(
            script,
            "UPDATE custom_equipment.weapons",
            "ALTER TABLE custom_equipment.weapons ALTER COLUMN display_name_key SET NOT NULL"
        );
        AssertAppearsBefore(
            script,
            "UPDATE custom_equipment.gameplay_items",
            "ALTER TABLE custom_equipment.gameplay_items ALTER COLUMN display_name_key SET NOT NULL"
        );
        AssertAppearsBefore(
            script,
            "UPDATE custom_equipment.shop_settings",
            "ALTER TABLE custom_equipment.shop_settings ALTER COLUMN display_name_key SET NOT NULL"
        );
        Assert.Contains("Equipment.Item.", script);
        Assert.Contains("Equipment.Shop.", script);
        Assert.Contains("CK_weapons_image_url", script);
        Assert.Contains("CK_gameplay_items_image_url", script);
        Assert.DoesNotContain(
            "REFERENCES localization.",
            script,
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Theory]
    [InlineData(typeof(WeaponEntity), nameof(WeaponEntity.DisplayNameKey), 191, false)]
    [InlineData(typeof(WeaponEntity), nameof(WeaponEntity.ImageUrl), 2048, true)]
    [InlineData(typeof(GameplayItemEntity), nameof(GameplayItemEntity.DisplayNameKey), 191, false)]
    [InlineData(typeof(GameplayItemEntity), nameof(GameplayItemEntity.ImageUrl), 2048, true)]
    [InlineData(typeof(EquipmentShopSettingsEntity), nameof(EquipmentShopSettingsEntity.DisplayNameKey), 191, false)]
    [InlineData(typeof(EquipmentShopCategoryEntity), nameof(EquipmentShopCategoryEntity.DisplayNameKey), 191, false)]
    [InlineData(typeof(EquipmentShopCategoryEntity), nameof(EquipmentShopCategoryEntity.DescriptionKey), 191, true)]
    [InlineData(typeof(EquipmentShopListingEntity), nameof(EquipmentShopListingEntity.DescriptionKey), 191, true)]
    [InlineData(typeof(EquipmentShopProductEntity), nameof(EquipmentShopProductEntity.DisplayNameKey), 191, false)]
    public void RuntimeModel_ExposesLocalizationAndImageContract(
        Type entityType,
        string propertyName,
        int maxLength,
        bool nullable
    )
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(entityType)?.FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(maxLength, property!.GetMaxLength());
        Assert.Equal(nullable, property.IsNullable);
    }

    private static CustomEquipmentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CustomEquipmentDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=custom_equipment_migration_test;Username=test;Password=test"
            )
            .Options;
        return new CustomEquipmentDbContext(options);
    }

    private static string GenerateScript(string fromMigration, string toMigration)
    {
        using var context = CreateContext();
        return context.GetService<IMigrator>().GenerateScript(fromMigration, toMigration);
    }

    private static void AssertAppearsBefore(string text, string first, string second)
    {
        var firstIndex = text.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = text.IndexOf(second, StringComparison.Ordinal);

        Assert.True(firstIndex >= 0, $"SQL fragment not found: {first}");
        Assert.True(secondIndex >= 0, $"SQL fragment not found: {second}");
        Assert.True(firstIndex < secondIndex, $"Expected '{first}' before '{second}'.");
    }
}
