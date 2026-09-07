using CustomHud.Api;
using Xunit;

namespace CustomHud.Core.Tests;

public sealed class HudMarkupTests
{
    [Fact]
    public void NestedHtmlRestoresStyleAndPreservesLineBreaks()
    {
        var document = Parse("<font color='#85dcb1'>A<b>B</b>C</font><br><i>D</i>E");
        var first = document.Lines[0];
        Assert.Equal(new[] { "A", "B", "C" }, first.Select(run => run.Text));
        Assert.True(HudPalette.TryResolve("mint", out var mint));
        Assert.All(first, run => Assert.Equal(mint, run.Style.Color));
        Assert.True(first[1].Style.Bold);
        Assert.False(first[2].Style.Bold);
        Assert.True(document.Lines[1][0].Style.Italic);
        Assert.Equal(HudRunStyle.Default, document.Lines[1][1].Style);
    }

    [Fact]
    public void SpanColorAndLocalizationTagsUseTheSamePalette()
    {
        var html = Parse("<span style='color: #85dcb1'><u>A</u></span>").Lines[0][0];
        var chat = Parse("[mint]A[/]B").Lines[0];
        Assert.Equal(html.Style.Color, chat[0].Style.Color);
        Assert.True(html.Style.Underline);
        Assert.Equal(HudPalette.White, chat[1].Style.Color);
    }

    [Fact]
    public void EscapedPlayerNameCannotInjectFormatting()
    {
        const string name = "<b>[red]Игрок & друг[/]</b>";
        var document = Parse(HudText.Escape(name));
        Assert.Equal(name, Text(document));
        Assert.All(document.Lines.SelectMany(line => line), run => Assert.Equal(HudRunStyle.Default, run.Style));
    }

    [Fact]
    public void PlainTextDoesNotInterpretEntitiesOrTags()
    {
        const string literal = "<b>[red]A &amp; B[/]</b>";
        var document = HudMarkup.Parse(literal, HudTextFormat.PlainText, HudMessageStyle.Notice);
        Assert.Equal(literal, Text(document));
        Assert.Equal(HudRunStyle.Default, Assert.Single(document.Lines[0]).Style);
    }

    [Fact]
    public void UnsupportedAttributesDoNotBecomeCssOrExecutableContent()
    {
        var document = Parse("<span style='color: red' onclick='attack()'>text</span><img src='remote' />");
        Assert.Equal("text", Text(document));
        Assert.True(HudPalette.TryResolve("red", out var red));
        Assert.Equal(red, document.Lines[0][0].Style.Color);
    }

    [Fact]
    public void LongTextAndAlternatingStylesAreBoundedWithoutSplittingSurrogates()
    {
        var document = Parse(string.Concat(Enumerable.Repeat("[red]😀[blue]Я", 180)));
        Assert.Equal(HudMarkup.MaximumLines, document.Lines.Length);
        Assert.All(document.Lines, line => Assert.InRange(line.Length, 1, HudMarkup.MaximumRuns));
        Assert.EndsWith("…", Text(document));
        Assert.DoesNotContain("\uFFFD", Text(document));
    }

    [Fact]
    public void TooLongInputIsRejectedBeforeParsing()
    {
        Assert.Throws<ArgumentException>(() => Parse(new string('a', HudMarkup.MaximumInputLength + 1)));
    }

    [Theory]
    [InlineData("#123456")]
    [InlineData("#abc")]
    [InlineData("mint")]
    public void ColorUsesABoundedPrecompiledClass(string color)
    {
        Assert.True(HudPalette.TryResolve(color, out var index));
        Assert.InRange(index, 0, HudPalette.Colors.Length - 1);
    }

    private static HudDocument Parse(string text) => HudMarkup.Parse(text, HudTextFormat.Markup, HudMessageStyle.Notice);
    private static string Text(HudDocument document) => string.Join("\n", document.Lines.Select(line => string.Concat(line.Select(run => run.Text))));
}
