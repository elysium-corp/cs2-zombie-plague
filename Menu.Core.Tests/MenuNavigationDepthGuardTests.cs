using Menu.Core.Runtime;

namespace Menu.Core.Tests;

public sealed class MenuNavigationDepthGuardTests
{
    [Fact]
    public void TryAdvance_StopsAtConfiguredMaximumDepth()
    {
        Assert.True(MenuNavigationDepthGuard.TryAdvance(0, 2, out var first));
        Assert.True(MenuNavigationDepthGuard.TryAdvance(first, 2, out var second));
        Assert.False(MenuNavigationDepthGuard.TryAdvance(second, 2, out var stopped));

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(2, stopped);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public void Back_NeverReturnsNegativeDepth(int current, int expected)
    {
        Assert.Equal(expected, MenuNavigationDepthGuard.Back(current));
    }
}
