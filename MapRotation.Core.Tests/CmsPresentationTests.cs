using MapRotation.Core.Database;
using MapRotation.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class CmsPresentationTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("https://example.com/map.png", false)]
    [InlineData("panorama/images/../map.vtex", false)]
    [InlineData("panorama/images/custom_game/elysium/assets/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa_png.vtex", true)]
    public void CatalogOnlyAcceptsCompiledCmsImagePaths(string? path, bool valid)
    {
        var map = new RotationMap { Id = 1, Key = "lila", MapName = "zm_lila", DisplayName = "Lila", HudImagePath = path };
        Assert.Equal(valid, map.IsSafe);
    }

    [Fact]
    public void MigrationAndSnapshotMatchTheRuntimeModel()
    {
        using var db = new MapRotationDbContextDesignTimeFactory().CreateDbContext([]);
        Assert.False(db.Database.HasPendingModelChanges());
        var script = db.Database.GenerateCreateScript();
        Assert.Contains("hud_image_path character varying(256)", script);
        Assert.Contains("hud_settings jsonb", script);
        Assert.Contains("menu_items_per_page integer", script);
        Assert.Contains("20260924150000_AddMapRotationCmsPresentation", db.Database.GetMigrations());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void PageSizeMatchesTheHudSlotContract(int size) =>
        Assert.Throws<ArgumentException>(() => new RotationSettings { MenuItemsPerPage = size }.Validate());
}
