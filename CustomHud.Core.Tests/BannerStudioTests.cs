using Xunit;
using CustomHud.Api;

namespace CustomHud.Core.Tests;

public sealed class BannerStudioTests
{
    [Fact]
    public void TitleOnlyCompositionHasNoPhantomDescription()
    {
        var design = new HudBannerTemplate { Variant = "custom", ShowHeader = false, ShowTitle = true, ShowDescription = false, TitleSize = 40, WidthPixels = 800 };
        var document = HudBannerDesign.Parse(design, new() { Title = "Инфекция" }, HudTextFormat.Markup);
        Assert.NotEmpty(document.Banner!.Title);
        Assert.Empty(document.Banner.Header);
        Assert.DoesNotContain(document.Lines, line => line.Any(run => !string.IsNullOrEmpty(run.Text)));
    }

    [Fact]
    public void FineSizingAndHiddenFieldsAreValidatedBeforeDelivery()
    {
        var design = new HudBannerTemplate { Variant = "custom", ShowHeader = false, ShowTitle = false };
        Assert.Throws<ArgumentException>(() => HudBannerDesign.Parse(design with { WidthPixels = 333 }, new() { Description = "x" }, HudTextFormat.Markup));
        Assert.Throws<ArgumentException>(() => HudBannerDesign.Parse(design, new() { Header = "hidden", Description = "x" }, HudTextFormat.Markup));
        Assert.Throws<ArgumentException>(() => HudBannerDesign.Parse(design with { ShowDescription = false }, new(), HudTextFormat.Markup));
        var large = HudBannerDesign.TextWidth(design with { WidthPixels = 800, DescriptionSize = 18, Padding = 8 }, "Description");
        var small = HudBannerDesign.TextWidth(design with { WidthPixels = 320, DescriptionSize = 32, Padding = 40 }, "Description");
        Assert.True(large > small);
    }
}
