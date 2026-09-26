using CustomEquipment.Api.Data;
using CustomEquipment.Services;
using Moq;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class GrenadeThrowTrackerTests
{
    [Fact]
    public void ThrowSurvivesRemovalOfInventoryEntityAndIsConsumedOnce()
    {
        var tracker = new GrenadeThrowTracker();
        var grenade = new Mock<GrenadeItemBase>(MockBehavior.Strict).Object;
        tracker.Capture(100, "incgrenade", grenade, 1000);

        // Объект снаряда разрешается без обращения к уже удалённой AttachedEntity.
        Assert.Same(grenade, tracker.TryTake(100, "incgrenade", 1016));
        Assert.Null(tracker.TryTake(100, "incgrenade", 1016));
    }

    [Fact]
    public void ThrowDoesNotMatchAnotherPawnOrWeapon()
    {
        var tracker = new GrenadeThrowTracker();
        var grenade = new Mock<GrenadeItemBase>(MockBehavior.Strict).Object;
        tracker.Capture(100, "incgrenade", grenade, 1000);

        Assert.Null(tracker.TryTake(101, "incgrenade", 1016));
        Assert.Null(tracker.TryTake(100, "molotov", 1016));
        Assert.Same(grenade, tracker.TryTake(100, "incgrenade", 1016));
    }

    [Fact]
    public void ExpiredOrClearedThrowCannotCustomizeALaterProjectile()
    {
        var tracker = new GrenadeThrowTracker();
        var grenade = new Mock<GrenadeItemBase>(MockBehavior.Strict).Object;
        tracker.Capture(100, "incgrenade", grenade, 1000);
        Assert.Null(tracker.TryTake(100, "incgrenade", 3000));

        tracker.Capture(100, "incgrenade", grenade, 4000);
        tracker.Clear();
        Assert.Null(tracker.TryTake(100, "incgrenade", 4016));
    }
}
