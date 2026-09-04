using System.Collections.Frozen;
using Advertisement.Core.Data;

namespace Advertisement.Core.Tests;

public sealed class DeliveryRuleTests
{
    [Theory]
    [InlineData(21, 30, true)]
    [InlineData(1, 30, true)]
    [InlineData(12, 0, false)]
    public void IsActive_WhenDailyWindowCrossesMidnight_UsesBothParts(
        int hour,
        int minute,
        bool expected)
    {
        var message = CreateMessage(
            dailyStartTime: new TimeOnly(20, 0),
            dailyEndTime: new TimeOnly(2, 0));
        var local = new DateTime(2026, 8, 27, hour, minute, 0, DateTimeKind.Unspecified);
        var timestamp = new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));

        var active = message.IsActive(timestamp, 10);

        Assert.Equal(expected, active);
    }

    [Fact]
    public void ParseDailyTimes_IgnoresInvalidValuesAndRemovesDuplicates()
    {
        var values = DeliveryRuleParser.ParseDailyTimes(["09:15", "bad", "09:15", "22:30"]);

        Assert.Equal(2, values.Count);
        Assert.True(values.Contains(new TimeOnly(9, 15)));
        Assert.True(values.Contains(new TimeOnly(22, 30)));
    }

    [Theory]
    [InlineData("periodic", "Periodic")]
    [InlineData("daily", "Daily")]
    [InlineData("manual", "Manual")]
    [InlineData("unknown", "Periodic")]
    public void ParseDispatchMode_ReturnsSafeValue(
        string value,
        string expected)
    {
        Assert.Equal(expected, DeliveryRuleParser.ParseDispatchMode(value).ToString());
    }

    private static AdvertisementMessage CreateMessage(
        TimeOnly? dailyStartTime,
        TimeOnly? dailyEndTime)
    {
        return new AdvertisementMessage(
            1,
            "test",
            "Test",
            "Advertisement.Messages.Test",
            null,
            "information",
            true,
            0,
            100,
            0,
            null,
            AdvertisementDispatchMode.Periodic,
            Array.Empty<TimeOnly>().ToFrozenSet(),
            dailyStartTime,
            dailyEndTime,
            AdvertisementAudienceType.All,
            null,
            null,
            null,
            null,
            null);
    }
}
