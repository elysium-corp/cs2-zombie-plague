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
    }

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
