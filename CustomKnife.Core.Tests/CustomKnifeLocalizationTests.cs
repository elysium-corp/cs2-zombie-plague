using CustomKnife.Data.Knives;
using CustomKnife.Database;
using CustomKnife.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace CustomKnife.Core.Tests;

public sealed class CustomKnifeLocalizationTests
{
    private const string PreviousMigration = "20260901090000_CreateKnifeCatalog";
    private const string FeatureMigration = "20260901220500_AddKnifeLocalizationAndImage";

    [Fact]
    public void UpgradeScript_BackfillsKeysBeforeMakingThemRequired()
    {
        var script = GenerateScript();

        AssertAppearsBefore(
            script,
            "UPDATE custom_knife.knives",
            "ALTER TABLE custom_knife.knives ALTER COLUMN display_name_key SET NOT NULL"
        );
        AssertAppearsBefore(
            script,
            "UPDATE custom_knife.knives",
            "ALTER TABLE custom_knife.knives ALTER COLUMN description_key SET NOT NULL"
        );
        Assert.Contains("CustomKnife.", script);
        Assert.Contains("CK_knives_localization_keys", script);
        Assert.Contains("CK_knives_image_url", script);
        Assert.DoesNotContain(
            "REFERENCES localization.",
            script,
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public void RuntimeModel_RequiresKeysAndKeepsImageOptional()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(KnifeEntity));

        Assert.NotNull(entity);
        AssertProperty(entity!, nameof(KnifeEntity.DisplayNameKey), 191, nullable: false);
        AssertProperty(entity!, nameof(KnifeEntity.DescriptionKey), 191, nullable: false);
        AssertProperty(entity!, nameof(KnifeEntity.ImageUrl), 2048, nullable: true);
    }

    [Fact]
    public void FallbackKnife_UsesExplicitLocalizationKeys()
    {
        Assert.Equal("CustomKnife.knife_axe.Name", KnifeDefaults.Fallback.DisplayNameKey);
        Assert.Equal("CustomKnife.knife_axe.Description", KnifeDefaults.Fallback.DescriptionKey);
    }

    private static void AssertProperty(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entity,
        string propertyName,
        int maxLength,
        bool nullable
    )
    {
        var property = entity.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(maxLength, property!.GetMaxLength());
        Assert.Equal(nullable, property.IsNullable);
    }

    private static CustomKnifeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CustomKnifeDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=custom_knife_migration_test;Username=test;Password=test"
            )
            .Options;
        return new CustomKnifeDbContext(options);
    }

    private static string GenerateScript()
    {
        using var context = CreateContext();
        return context.GetService<IMigrator>().GenerateScript(
            PreviousMigration,
            FeatureMigration
        );
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
