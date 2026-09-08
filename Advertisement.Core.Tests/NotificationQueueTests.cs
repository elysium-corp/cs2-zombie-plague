using Xunit;
using Advertisement.Core.Application;
using CustomHud.Api;

namespace Advertisement.Core.Tests;

public sealed class NotificationQueueTests
{
    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        internal void Advance(double seconds) => _now = _now.AddSeconds(seconds);
    }
    private static BannerNotificationRule Rule(string key = "test", string delivery = "queue", int priority = 100) =>
        new() { EventKey = key, Delivery = delivery, Options = new() { DurationSeconds = 1, Priority = priority }, Template = new() { Exit = "none" } };

    [Fact]
    public void QueueGivesEveryAwardItsFullVisibleDuration()
    {
        var clock = new Clock(); var queue = new NotificationQueue(clock);
        queue.Enqueue(1, 11, Rule("human"), new Dictionary<string, object?> { ["round"] = 7 });
        queue.Enqueue(1, 11, Rule("zombie"), new Dictionary<string, object?> { ["round"] = 7 });
        var first = Assert.Single(queue.Ready()); Assert.Equal("human", first.Rule.EventKey); queue.Shown(first);
        clock.Advance(.5); Assert.Empty(queue.Ready());
        clock.Advance(.5); var second = Assert.Single(queue.Ready()); Assert.Equal("zombie", second.Rule.EventKey);
        Assert.Equal(7, second.Parameters["round"]);
    }

    [Fact]
    public void RepeatedDamageCoalescesAndDoesNotRestartAppearanceOrSound()
    {
        var queue = new NotificationQueue(new Clock()); var rule = Rule("damage", "replace");
        queue.Enqueue(1, 11, rule, new Dictionary<string, object?> { ["damage"] = 10 });
        queue.Enqueue(1, 11, rule, new Dictionary<string, object?> { ["damage"] = 25 });
        var first = Assert.Single(queue.Ready()); Assert.Equal(25, first.Parameters["damage"]); Assert.False(first.IsUpdate); queue.Shown(first);
        queue.Enqueue(1, 11, rule, new Dictionary<string, object?> { ["damage"] = 50 });
        Assert.True(Assert.Single(queue.Ready()).IsUpdate);
    }

    [Fact]
    public void HighPriorityPreemptsWhileOldQueueEntriesExpire()
    {
        var clock = new Clock(); var queue = new NotificationQueue(clock);
        queue.Enqueue(1, 11, Rule("active") with { Options = new() { DurationSeconds = 60, Priority = 10 } }, new Dictionary<string, object?>());
        queue.Shown(Assert.Single(queue.Ready()));
        queue.Enqueue(1, 11, Rule("expired", priority: 1) with { MaxQueueAgeSeconds = 1 }, new Dictionary<string, object?>());
        queue.Enqueue(1, 11, Rule("urgent", priority: 100), new Dictionary<string, object?>());
        var urgent = Assert.Single(queue.Ready()); Assert.Equal("active", urgent.PreviousEvent); queue.Shown(urgent);
        clock.Advance(2); Assert.Empty(queue.Ready());
    }

    [Fact]
    public void DisconnectAndSlotReuseNeverDeliverThePreviousPlayersMessages()
    {
        var queue = new NotificationQueue(new Clock());
        queue.Enqueue(1, 11, Rule("old"), new Dictionary<string, object?>());
        queue.Enqueue(1, 22, Rule("new"), new Dictionary<string, object?>());
        Assert.Equal("new", Assert.Single(queue.Ready()).Rule.EventKey);
        queue.Enqueue(1, 22, Rule(), new Dictionary<string, object?>()); queue.Disconnect(1);
        Assert.Empty(queue.Ready()); Assert.Empty(queue.EventKeys);
    }

    [Fact]
    public void CooldownAndExplicitClearHavePredictableBoundaries()
    {
        var clock = new Clock(); var queue = new NotificationQueue(clock);
        var rule = Rule() with { CooldownSeconds = 2 };
        Assert.True(queue.Enqueue(1, 11, rule, new Dictionary<string, object?>()));
        Assert.False(queue.Enqueue(1, 11, rule, new Dictionary<string, object?>()));
        clock.Advance(2); Assert.True(queue.Enqueue(1, 11, rule, new Dictionary<string, object?>()));
        queue.ClearEvent(rule.EventKey); Assert.Empty(queue.Ready());
        Assert.True(queue.Enqueue(1, 11, rule, new Dictionary<string, object?>()));
    }

    [Fact]
    public void StackShowsThreeImmediatelyThenWaitsOnlyForOneFreePlace()
    {
        var clock = new Clock(); var queue = new NotificationQueue(clock);
        for (var i = 0; i < 4; i++) queue.Enqueue(1, 11, Rule("info", "stack"), new Dictionary<string, object?> { ["index"] = i });
        var ready = queue.Ready(); Assert.Equal(3, ready.Length);
        foreach (var item in ready) queue.Shown(item);
        clock.Advance(.5); Assert.Empty(queue.Ready());
        clock.Advance(.5); Assert.Equal(3, Assert.Single(queue.Ready()).Parameters["index"]);
        queue.ClearEvent("info"); Assert.Empty(queue.EventKeys);
    }

    [Fact]
    public void StackCanAppearUnderAnActiveRoundWhileQueueStillWaits()
    {
        var queue = new NotificationQueue(new Clock());
        queue.Enqueue(1, 11, Rule("round"), new Dictionary<string, object?>()); queue.Shown(Assert.Single(queue.Ready()));
        queue.Enqueue(1, 11, Rule("queued"), new Dictionary<string, object?>());
        queue.Enqueue(1, 11, Rule("info", "stack"), new Dictionary<string, object?>());
        Assert.Equal("info", Assert.Single(queue.Ready()).Rule.EventKey);
    }

    [Fact]
    public void RoundAnnouncementWinsEvenWhenFirstInfectedWasPublishedFirst()
    {
        var queue = new NotificationQueue(new Clock());
        var defaults = NotificationCatalog.Defaults["Game.Round.Started"];
        var round = defaults with { Options = defaults.Options with { Priority = 100 } };
        var first = NotificationCatalog.Defaults["ZombiePlague.Round.Infection.FirstInfected"];
        queue.Enqueue(1, 11, first with { Delivery = "queue", Options = first.Options with { Position = round.Options.Position } }, new Dictionary<string, object?>());
        queue.Enqueue(1, 11, round, new Dictionary<string, object?>());
        Assert.Equal("Game.Round.Started", Assert.Single(queue.Ready()).Rule.EventKey);
        Assert.NotEqual(round.Options.Position, first.Options.Position);
    }

    [Fact]
    public void CatalogDefaultsAreCompleteAndNeverContainTheRemovedCooldownPopup()
    {
        Assert.DoesNotContain("ZombiePlague.Ability.Cooldown", NotificationCatalog.Defaults.Keys);
        Assert.True(NotificationCatalog.Defaults.Count >= 50);
        foreach (var rule in NotificationCatalog.Defaults.Values) NotificationCatalog.Validate(rule);
        Assert.Contains("roundName", NotificationCatalog.Aliases["round_name"]);
    }
}
