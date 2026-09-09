using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.Api.Data;
using Shop.Core.Configuration;
using Shop.Core.Data;
using Shop.Core.Database;
using Shop.Core.Database.Entities;
using Shop.Core.Hud;

namespace Shop.Core.Tests;

public sealed class ShopHudAppearanceTests
{
    [Fact]
    public void DatabaseAndFallbackPreserveTheSameAppearanceAndOldFilesUseDefaults()
    {
        const string json = """{"theme":"tactical","columns":3,"rows":2,"wrapPages":true,"accent":"gold"}""";
        var db = ShopSnapshotMapper.FromDatabase([
            new ShopStorefrontEntity { ShopType = "human", TitleKey = "Shop.Human.Title", Enabled = true, HudSettingsJson = json },
            new ShopStorefrontEntity { ShopType = "zombie", TitleKey = "Shop.Zombie.Title", Enabled = true }
        ], [], []);
        var fallback = ShopSnapshotMapper.FromFallback(new ShopFallbackConfig
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Storefronts = [
                new ShopFallbackStorefront { ShopType = "human", TitleKey = "Shop.Human.Title", HudSettingsJson = json },
                new ShopFallbackStorefront { ShopType = "zombie", TitleKey = "Shop.Zombie.Title" }
            ]
        });
        Assert.Equal(db.Storefronts[ShopType.Human].Appearance, fallback.Storefronts[ShopType.Human].Appearance);
        Assert.Equal(ShopHudAppearance.Default, fallback.Storefronts[ShopType.Zombie].Appearance);
    }

    [Fact]
    public void HumanFrameIsSharedWithoutReplacingZombieThemeOrCapacity()
    {
        var snapshot = ShopSnapshotMapper.FromFallback(new ShopFallbackConfig
        {
            GeneratedAt = DateTimeOffset.Parse("2026-09-09T12:00:00Z"),
            Storefronts = [
                new ShopFallbackStorefront { ShopType = "human", TitleKey = "Human", HudSettingsJson = """{"width":1120,"height":760,"defaultScale":85}""" },
                new ShopFallbackStorefront { ShopType = "zombie", TitleKey = "Zombie", HudSettingsJson = """{"width":1720,"height":980,"theme":"tactical","columns":1,"rows":1}""" }
            ]
        });
        var zombie = snapshot.Storefronts[ShopType.Zombie].Appearance.WithFrame(snapshot.Frame);
        Assert.Equal((1120, 760, 85), (zombie.Width, zombie.Height, zombie.DefaultScale));
        Assert.Equal(("tactical", 1, 1), (zombie.Theme, zombie.Columns, zombie.Rows));
        Assert.Empty(snapshot.Offers);
    }

    [Fact]
    public void AllCmsOptionsHaveMatchingPanoramaClassesAndDefaults()
    {
        using var spec = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "hud-appearance.json")));
        Assert.Equal(ShopHudAppearance.Default, ShopHudAppearance.Parse(spec.RootElement.GetProperty("defaults").GetRawText()));
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "elysium_shop_v4_r4.css"));
        foreach (var field in spec.RootElement.GetProperty("options").EnumerateObject())
        foreach (var option in field.Value.EnumerateArray())
        {
            var value = ShopHudAppearance.Parse("{\"" + field.Name + "\":" + option.GetRawText() + "}");
            foreach (var (_, name) in value.Classes())
            {
                if (name is "Theme_elysium" or "Icons_glow" or "Highlight_rarity" or "Page_none") continue;
                Assert.Contains("." + name, css);
            }
        }
    }

    [Fact]
    public void WebPreviewIsGeneratedFromCurrentGameResourcesAndSpecification()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        using var template = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "shop-hud-template.json")));
        var source = File.ReadAllText(Path.Combine(directory, "elysium_shop_v4_r4.xml"))
            + File.ReadAllText(Path.Combine(directory, "elysium_shop_v4_r4.css"));
        Assert.Equal(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(source.Replace("\r\n", "\n", StringComparison.Ordinal)))),
            template.RootElement.GetProperty("sourceSha256").GetString());
        Assert.Equal(ShopHudCatalog.ColumnCount, template.RootElement.GetProperty("columns").GetInt32());
        Assert.Equal(ShopHudCatalog.RowCount, template.RootElement.GetProperty("rows").GetInt32());
        using var specification = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "hud-appearance.json")));
        Assert.True(JsonElement.DeepEquals(specification.RootElement, template.RootElement.GetProperty("specification")));
    }

    [Theory]
    [InlineData("{\"theme\":\"custom css\"}")]
    [InlineData("{\"columns\":9}")]
    [InlineData("{\"rows\":0}")]
    [InlineData("{\"accent\":\"#fff\"}")]
    public void InvalidAppearanceRejectsSnapshotInsteadOfSendingCssToClients(string json) =>
        Assert.Throws<InvalidDataException>(() => ShopHudAppearance.Parse(json));

    [Theory]
    [InlineData(0, 3, -1, true, 2)]
    [InlineData(2, 3, 1, true, 0)]
    [InlineData(0, 3, -1, false, 0)]
    [InlineData(2, 3, 1, false, 2)]
    [InlineData(0, 1, -1, true, 0)]
    public void NavigationWrapsOnlyWhenEnabled(int page, int count, int direction, bool wrap, int expected) =>
        Assert.Equal(expected, ShopHudAppearance.MovePage(page, count, direction, wrap));

    [Fact]
    public void MigrationKeepsExistingStorefrontsAndAddsAnEmptyJsonDefault()
    {
        using var context = new ShopDbContext(new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test").Options);
        var sql = context.GetService<IMigrator>().GenerateScript(
            "20260909020000_AddShopPlayerPreferences", "20260909121000_AddShopHudAppearance");
        Assert.Contains("ALTER TABLE shop.storefronts ADD hud_settings jsonb NOT NULL DEFAULT", sql);
        Assert.Contains("'{}'::jsonb", sql);
        Assert.DoesNotContain("DELETE", sql);
    }
}
