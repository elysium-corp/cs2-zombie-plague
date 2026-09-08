using CustomHud.Api;
using Xunit;

namespace CustomHud.Core.Tests;

public sealed class HudMessageStoreTests
{
    private static readonly HudDocument Document = HudMarkup.Parse("text", HudTextFormat.PlainText, HudMessageStyle.Notice);

    [Fact]
    public void BannerWinsAndAdResumesOnlyWhileItsOriginalLifetimeRemains()
    {
        var clock = new Clock();
        var store = new HudMessageStore(clock);
        store.Put(1, 11, Document, new() { Channel = "ad", DurationSeconds = 10 });
        store.Put(1, 11, Document, new() { Channel = "round", DurationSeconds = 3, Priority = 100 });
        Assert.Equal("round", Current(store)?.Options.Channel);
        clock.Advance(3);
        Assert.Equal("ad", Current(store)?.Options.Channel);
        clock.Advance(7);
        Assert.Null(Current(store));
    }

    [Fact]
    public void AdThatExpiredBehindBannerNeverFlashesAfterIt()
    {
        var clock = new Clock();
        var store = new HudMessageStore(clock);
        store.Put(1, 11, Document, new() { Channel = "ad", DurationSeconds = 2 });
        store.Put(1, 11, Document, new() { Channel = "round", DurationSeconds = 6, Priority = 100 });
        clock.Advance(6);
        Assert.Null(Current(store));
    }

    [Fact]
    public void ChannelsAndPositionsAreIndependentAndHideCannotClearAnotherOwner()
    {
        var store = new HudMessageStore(new Clock());
        store.Put(1, 11, Document, new() { Channel = "ad", Position = HudPosition.BottomLeft });
        store.Put(1, 11, Document, new() { Channel = "round" });
        Assert.Equal(2, store.GetFrame(1, 11).Count(message => message is not null));
        store.Hide(1, 11, "ad");
        Assert.Equal("round", Assert.Single(store.GetFrame(1, 11).OfType<HudMessage>()).Options.Channel);
    }

    [Fact]
    public void ReplacementAndEqualPriorityAlwaysPreferNewestRevision()
    {
        var store = new HudMessageStore(new Clock());
        store.Put(1, 11, Document, new() { Channel = "a" });
        store.Put(1, 11, Document, new() { Channel = "b" });
        Assert.Equal("b", Current(store)?.Options.Channel);
        store.Put(1, 11, Document, new() { Channel = "a" });
        Assert.Equal("a", Current(store)?.Options.Channel);
        store.Hide(1, 11, "a");
        Assert.Equal("b", Current(store)?.Options.Channel);
    }

    [Fact]
    public void SlotReuseAndMapClearDoNotLeakMessages()
    {
        var store = new HudMessageStore(new Clock());
        store.Put(1, 11, Document, new());
        Assert.All(store.GetFrame(1, 22), Assert.Null);
        Assert.Null(Current(store));
        store.Put(1, 22, Document, new());
        store.Disconnect(1);
        Assert.All(store.GetFrame(1, 22), Assert.Null);
        store.Put(1, 22, Document, new());
        store.Clear();
        Assert.All(store.GetFrame(1, 22), Assert.Null);
    }

    [Fact]
    public void ChannelCleanupAlsoRemovesCoveredMessagesForAllPlayers()
    {
        var store = new HudMessageStore(new Clock());
        foreach (var player in new[] { 1, 2 })
        {
            store.Put(player, 11, Document, new() { Channel = "ad" });
            store.Put(player, 11, Document, new() { Channel = "round", Priority = 100 });
        }
        store.ClearChannel("ad");
        store.ClearChannel("round");
        Assert.All(store.GetFrame(1, 11), Assert.Null);
        Assert.All(store.GetFrame(2, 11), Assert.Null);
    }

    [Fact]
    public void CapacityIsBoundedButAnExistingChannelCanStillRefresh()
    {
        var clock = new Clock();
        var store = new HudMessageStore(clock);
        for (var i = 0; i < HudMessageStore.MaximumMessagesPerPlayer; i++)
            Assert.True(store.Put(1, 11, Document, new() { Channel = "owner" + i }));
        Assert.False(store.Put(1, 11, Document, new() { Channel = "overflow" }));
        Assert.True(store.Put(1, 11, Document, new() { Channel = "owner0" }));
        clock.Advance(6);
        Assert.True(store.Put(1, 11, Document, new() { Channel = "new" }));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(61)]
    public void InvalidLifetimesAreRejected(double seconds) =>
        Assert.Throws<ArgumentException>(() => HudMessageStore.Validate(new() { DurationSeconds = seconds }));

    [Fact]
    public void StacksKeepRepeatedEventsInOrderWithBoundedCapacityAndOwnerCleanup()
    {
        var clock = new Clock(); var store = new HudMessageStore(clock);
        var options = new HudMessageOptions { Channel = "award", Stack = true, DurationSeconds = 2 };
        Assert.True(store.Put(1, 11, Document, options));
        Assert.True(store.Put(1, 11, Document, options));
        Assert.True(store.Put(1, 11, Document, options));
        Assert.False(store.Put(1, 11, Document, options));
        Assert.Equal(new long[] { 1, 2, 3 }, store.GetStackedFrame(1, 11).OfType<HudMessage>().Select(message => message.Revision));
        store.Hide(1, 11, "other"); Assert.Equal(3, store.GetStackedFrame(1, 11).OfType<HudMessage>().Count());
        store.Hide(1, 11, "award"); Assert.Empty(store.GetStackedFrame(1, 11).OfType<HudMessage>());
        store.Put(1, 11, Document, options); clock.Advance(2);
        Assert.Empty(store.GetStackedFrame(1, 11).OfType<HudMessage>());
    }

    private static HudMessage? Current(HudMessageStore store) => store.GetFrame(1, 11)[(int)HudPosition.TopCenter];

    internal sealed class Clock : TimeProvider
    {
        private long _ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _ticks;
        internal void Advance(double seconds) => _ticks += TimeSpan.FromSeconds(seconds).Ticks;
    }
}
