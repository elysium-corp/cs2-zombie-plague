using System.Text.Json;
using Shop.Core.Hud;

namespace Shop.Core.Tests;

public sealed class ShopHudCustomizationTests
{
    [Theory]
    [InlineData(0, "clock", "00:00")]
    [InlineData(9, "clock", "00:09")]
    [InlineData(60, "clock", "01:00")]
    [InlineData(5999, "clock", "99:59")]
    [InlineData(6000, "clock", "2h")]
    [InlineData(59, "compact", "59s")]
    [InlineData(86400, "compact", "1d")]
    [InlineData(int.MaxValue, "clock", "999d+")]
    [InlineData(int.MaxValue, "seconds", "99999+")]
    public void CountdownLabelsStayBoundedAcrossDigitAndUnitTransitions(int seconds, string format, string expected) =>
        Assert.Equal(expected, ShopHudCountdown.Format(seconds, format));

    [Fact]
    public void CompactCountdownUsesPlayerLocalization() =>
        Assert.Equal("2ч", ShopHudCountdown.Format(6000, "compact", _ => "ч"));

    [Theory]
    [InlineData(60, 60, 100)]
    [InlineData(30, 60, 50)]
    [InlineData(1, 60, 2)]
    [InlineData(0, 60, 0)]
    [InlineData(60, 0, 0)]
    [InlineData(int.MaxValue, 1, 100)]
    public void ProgressCannotOverflowOrEscapeTheTimer(int remaining, int duration, int expected) =>
        Assert.Equal(expected, ShopHudCountdown.Progress(remaining, duration));

    [Fact]
    public void NestedRaritySettingsRoundTripWithoutAffectingOtherRarities()
    {
        const string json = """{"timerShape":"circle","timerPosition":"center","timerSpeed":"slow","rarityStyles":{"Rare":{"color":"#abcdef","glowWidth":12,"glowIntensity":180,"borderWidth":3,"fillOpacity":40,"hoverSound":"Elysium.Shop.Rare.Hover"}}}""";
        var appearance = ShopHudAppearance.Parse(json);
        var copy = ShopHudAppearance.Parse(JsonSerializer.Serialize(appearance, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        Assert.Equal(appearance, copy);
        Assert.Equal(ShopHudAppearance.Default.RarityStyles.Common, appearance.RarityStyles.Common);
        Assert.Equal("Elysium.Shop.Rare.Hover", appearance.RarityStyles.Rare.HoverSound);
    }

    [Theory]
    [InlineData("{\"timerShape\":\"arbitrary css\"}")]
    [InlineData("{\"timerSpeed\":\"infinite\"}")]
    [InlineData("{\"rarityStyles\":null}")]
    [InlineData("{\"rarityStyles\":{\"Rare\":null}}")]
    [InlineData("{\"rarityStyles\":{\"Rare\":{\"color\":\"#fff;\"}}}")]
    [InlineData("{\"rarityStyles\":{\"Rare\":{\"glowWidth\":99}}}")]
    [InlineData("{\"rarityStyles\":{\"Rare\":{\"fillOpacity\":-1}}}")]
    [InlineData("{\"clickSound\":\"x; exec autoexec\"}")]
    public void InvalidSettingsRejectTheSnapshot(string json) =>
        Assert.Throws<InvalidDataException>(() => ShopHudAppearance.Parse(json));

    [Theory]
    [InlineData("{\"rarityStyles\":{\"MyRarity\":{}}}")]
    [InlineData("{\"rarityStyles\":{\"Rare\":{\"surprise\":true}}}")]
    public void UnknownNestedFieldsAreNotSilentlyIgnored(string json) =>
        Assert.Throws<JsonException>(() => ShopHudAppearance.Parse(json));
}
