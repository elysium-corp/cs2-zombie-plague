using Shop.Core.Hud;

namespace Shop.Core.Tests;

public sealed class ShopHudTransitionTests
{
    [Fact]
    public void ItemPageAnimatesOnlyItsColumnAndCommitsOnceBetweenPhases()
    {
        var transition = new ShopHudTransition();
        var swaps = 0;
        transition.Begin("Cards2", -1, 10, ShopHudAppearance.Default, () => swaps++);
        Assert.Equal("MotionOutPrevious", transition.ClassFor("Cards2"));
        Assert.Equal("MotionIdle", transition.ClassFor("Cards1"));
        Assert.Equal("MotionIdle", transition.ClassFor("Columns"));
        Assert.False(transition.Advance(10.1));
        Assert.Equal(0, swaps);
        transition.Begin("Columns", 1, 10.1, ShopHudAppearance.Default, () => swaps += 100);
        Assert.True(transition.Advance(10.2));
        Assert.Equal(1, swaps);
        Assert.Equal("MotionInPrevious", transition.ClassFor("Cards2"));
        Assert.True(transition.Busy);
        Assert.True(transition.Advance(10.4));
        Assert.False(transition.Busy);
        Assert.False(transition.Advance(100));
        Assert.Equal(1, swaps);
        Assert.Equal("MotionIdle", transition.ClassFor("Cards2"));
    }

    [Fact]
    public void DisabledAnimationCommitsImmediatelyAndDelayedTickStillShowsEntry()
    {
        var transition = new ShopHudTransition();
        var swaps = 0;
        transition.Begin("Columns", 1, 0, ShopHudAppearance.Default with { PageAnimation = "none" }, () => swaps++);
        Assert.Equal(1, swaps);
        Assert.False(transition.Busy);
        transition.Begin("Columns", 1, 0, ShopHudAppearance.Default, () => swaps++);
        Assert.True(transition.Advance(100));
        Assert.Equal("MotionInNext", transition.ClassFor("Columns"));
        Assert.Equal(2, swaps);
        Assert.True(transition.Advance(101));
        Assert.False(transition.Busy);
    }
}
