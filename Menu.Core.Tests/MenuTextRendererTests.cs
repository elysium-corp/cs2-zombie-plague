using Menu.Api.Contracts;
using Menu.Core.Swiftly;

namespace Menu.Core.Tests;

public sealed class MenuTextRendererTests
{
    [Fact]
    public void Render_EscapesMarkupFromConfiguration()
    {
        var result = MenuTextRenderer.Render(
            new LocalizedText { Default = "<font color='#fff'>Admin</font>" },
            locale: "ru");

        Assert.DoesNotContain("<font", result, StringComparison.Ordinal);
        Assert.Contains("&lt;font", result, StringComparison.Ordinal);
        Assert.Contains("&lt;/font&gt;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_DoesNotSplitUnicodeSurrogatePairAtLimit()
    {
        var result = MenuTextRenderer.Render(
            new LocalizedText { Default = "A😀B" },
            locale: null,
            maxLength: 2);

        Assert.Equal("A", result);
    }
}
