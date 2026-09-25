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
        Assert.Contains("CREATE TABLE map_rotation.player_preferences", script);
        Assert.Contains("hud_scale IN (80, 100, 120)", script);
        Assert.Contains("display_name_key character varying(191)", script);
        Assert.Contains("dock_side IN ('left', 'right')", script);
        Assert.Contains("animation IN ('none', 'fast', 'normal', 'slow')", script);
        Assert.Contains("20260924150000_AddMapRotationCmsPresentation", db.Database.GetMigrations());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(12)]
    public void PageSizeAcceptsTwoRowsWithAtMostSixVoteOptions(int size) =>
        new RotationSettings { MenuItemsPerPage = size, VoteOptionsCount = 6 }.Validate();

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void PageSizeMatchesTheHudSlotContract(int size) =>
        Assert.Throws<ArgumentException>(() => new RotationSettings { MenuItemsPerPage = size }.Validate());

    [Fact]
    public void LegacyCatalogWithThirtyCandidatesUpgradesWithoutDisablingRotation()
    {
        var configuration = RotationConfiguration.Create(new() { VoteOptionsCount = 30, NominationSlots = 12 }, []);
        Assert.Equal(6, configuration.Settings.VoteOptionsCount);
        Assert.Equal(6, configuration.Settings.NominationSlots);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("Maps.Gorodok.Title", true)]
    [InlineData("maps.gorodok", false)]
    [InlineData("Maps.Gorodok_Title", false)]
    [InlineData("<b>Карта</b>", false)]
    [InlineData("Maps.Gorodok\n", false)]
    public void CatalogOnlyAcceptsLocalizationKeys(string? key, bool valid)
    {
        var map = new RotationMap { Id = 1, Key = "gorodok", MapName = "zm_gorodok", DisplayName = "Gorodok", DisplayNameKey = key };
        Assert.Equal(valid, map.IsSafe);
    }
}
