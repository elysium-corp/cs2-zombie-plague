using Menu.Core.Swiftly;
using SwiftlyS2.Shared.Menus;

namespace Menu.Core.Tests;

public sealed class SwiftlyButtonParserTests
{
    [Theory]
    [InlineData("Mouse1", KeyBind.Mouse1)]
    [InlineData("ctrl+E", KeyBind.Ctrl | KeyBind.E)]
    [InlineData("Weapon1+Grenade2", KeyBind.Weapon1 | KeyBind.Grenade2)]
    public void TryParse_AcceptsKnownNamedFlags(string value, KeyBind expected)
    {
        Assert.True(SwiftlyButtonParser.TryParse(value, out var parsed));
        Assert.Equal(expected, parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("999")]
    [InlineData("Mouse1+")]
    [InlineData("Unknown")]
    public void TryParse_RejectsUnknownOrNumericFlags(string value)
    {
        Assert.False(SwiftlyButtonParser.TryParse(value, out var parsed));
        Assert.Equal((KeyBind)0, parsed);
    }
}
