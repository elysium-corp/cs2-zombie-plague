using CustomHud.Api;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class RotationHudConfigurationTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("not json")]
    [InlineData("{\"resultDuration\":\"6\",\"defaultDock\":7}")]
    public void IncompleteOrInvalidAppearanceDoesNotDisableTheHud(string json)
    {
        var settings = RotationHudConfiguration.Parse(json);
        Assert.Equal(80, settings.Defaults.ScalePercent);
        Assert.Equal(HudMenuDockSide.Right, settings.Defaults.DockSide);
        Assert.Equal(HudMenuAnimation.Normal, settings.Defaults.Animation);
        Assert.Equal(6, settings.ResultDuration);
        Assert.Equal(24, settings.VerticalGap);
    }

    [Theory]
    [InlineData("{}", 24)]
    [InlineData("{\"cardWidth\":220,\"rowHeight\":86}", 24)]
    [InlineData("not json", 24)]
    [InlineData("[]", 24)]
    [InlineData("{\"verticalGap\":null}", 24)]
    [InlineData("{\"verticalGap\":\"12\"}", 24)]
    [InlineData("{\"verticalGap\":12.5}", 24)]
    [InlineData("{\"verticalGap\":true}", 24)]
    [InlineData("{\"verticalGap\":2147483648}", 24)]
    [InlineData("{\"verticalGap\":-10}", 0)]
    [InlineData("{\"verticalGap\":0}", 0)]
    [InlineData("{\"verticalGap\":12}", 12)]
    [InlineData("{\"verticalGap\":32}", 32)]
    [InlineData("{\"verticalGap\":999}", 32)]
    public void VerticalGapAcceptsIntegersAndKeepsOlderAppearanceCompatible(string json, int expected)
        => Assert.Equal(expected, RotationHudConfiguration.Parse(json).VerticalGap);

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(8, 8)]
    [InlineData(999, 15)]
    public void CmsAppearanceIsBoundedAndControlsNewPlayerDefaults(int duration, int expected)
    {
        var settings = RotationHudConfiguration.Parse($$"""{"defaultDock":"left","defaultAnimation":"slow","resultDuration":{{duration}}}""");
        Assert.Equal(HudMenuDockSide.Left, settings.Defaults.DockSide);
        Assert.Equal(HudMenuAnimation.Slow, settings.Defaults.Animation);
        Assert.Equal(expected, settings.ResultDuration);
    }
}
