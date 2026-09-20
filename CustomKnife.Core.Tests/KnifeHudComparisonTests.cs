using System.Globalization;
using CustomKnife.Data.Knives;
using CustomKnife.Hud;
using Xunit;

namespace CustomKnife.Core.Tests;

public sealed class KnifeHudComparisonTests
{
    [Fact]
    public void ComparesActualKnifeParametersIncludingLowerGravityInRed()
    {
        var current = KnifeDefaults.Fallback with { Speed = 250, Gravity = 800, DamageMultiplier = 1 };
        var selected = current with { Speed = 275, Gravity = 720, DamageMultiplier = 1.15f };
        var stats = KnifeHudComparison.Compare(current, selected);
        Assert.Equal(["Increase", "Increase", "Decrease", "Unchanged"], stats.Select(stat => stat.Direction));
        Assert.Equal(["+10%", "+15%", "−10%", "0%"], stats.Select(stat => stat.DeltaText(CultureInfo.InvariantCulture)));
        Assert.Equal("×1,15", stats[1].SelectedText(CultureInfo.GetCultureInfo("ru")));
        Assert.Equal("800", stats[2].CurrentText(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(0, 25, "Increase", "+25")]
    [InlineData(0, 0, "Unchanged", "0%")]
    [InlineData(100, 0, "Decrease", "−100%")]
    [InlineData(double.NaN, 1, "Unchanged", "—")]
    [InlineData(1, double.PositiveInfinity, "Unchanged", "—")]
    public void ZeroAndInvalidValuesDoNotProduceMisleadingPercentages(double current, double selected, string direction, string delta)
    {
        var stat = new KnifeHudStat("Speed", current, selected);
        Assert.Equal(direction, stat.Direction);
        Assert.Equal(delta, stat.DeltaText(CultureInfo.InvariantCulture));
    }
}
