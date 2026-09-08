using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CustomHud.Api;

namespace CustomHud.Core;

internal readonly record struct HudRunStyle(int Color, bool Bold = false, bool Italic = false, bool Underline = false, bool ExplicitColor = false, bool Parameter = false)
{
    internal static readonly HudRunStyle Default = new(HudPalette.White);
}

internal sealed record HudRun(string Text, HudRunStyle Style);
internal sealed record HudDocument(HudRun[][] Lines)
{
    internal HudBannerDocument? Banner { get; init; }
}

internal static class HudMarkup
{
    internal const int MaximumInputLength = 4096;
    internal const int MaximumLines = 4;
    internal const int MaximumRuns = 12;
    private const int MaximumDepth = 16;
    private static readonly Regex Tokens = new(@"<[^>]{0,160}>|\[[A-Za-z_#][A-Za-z0-9_#]*\]|\[/\]",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex ColorAttribute = new("(?:^|\\s)color\\s*=\\s*['\"]?(?<value>#[a-fA-F0-9]{3,6}|[a-zA-Z]+)",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
    private static readonly Regex StyleAttribute = new("(?:^|\\s)style\\s*=\\s*['\"][^'\"]*?color\\s*:\\s*(?<value>#[a-fA-F0-9]{3,6}|[a-zA-Z]+)",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    internal static HudDocument Parse(string text, HudTextFormat format, HudMessageStyle messageStyle, int maximumLines = MaximumLines, int? width = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > MaximumInputLength) throw new ArgumentException("HUD: текст длиннее 4096 символов", nameof(text));
        var glyphs = new List<(Rune Glyph, HudRunStyle Style)>();
        var style = HudRunStyle.Default;
        var stack = new List<(string Tag, HudRunStyle Previous)>();
        if (format == HudTextFormat.PlainText) Append(text, decode: false);
        else
        {
            var start = 0;
            foreach (Match token in Tokens.Matches(text))
            {
                Append(text[start..token.Index], decode: true);
                Apply(token.Value);
                start = token.Index + token.Length;
            }
            Append(text[start..], decode: true);
        }
        return Wrap(glyphs, width ?? (messageStyle == HudMessageStyle.Banner ? 40 : 52), maximumLines);

        void Append(string value, bool decode)
        {
            if (decode) value = WebUtility.HtmlDecode(value);
            foreach (var rune in value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').EnumerateRunes())
            {
                if (rune.Value == '\t') glyphs.Add((new Rune(' '), style));
                else if (!Rune.IsControl(rune) || rune.Value == '\n') glyphs.Add((rune, style));
            }
        }

        void Apply(string token)
        {
            if (token == "[/]") { stack.Clear(); style = HudRunStyle.Default; return; }
            if (token[0] == '[')
            {
                if (HudPalette.TryResolve(token[1..^1], out var color)) style = style with { Color = color, ExplicitColor = true };
                else Append(token, decode: false);
                return;
            }
            var body = token[1..^1].Trim();
            var closing = body.StartsWith('/');
            if (closing) body = body[1..].TrimStart();
            var tag = new string(body.TakeWhile(char.IsLetter).ToArray()).ToLowerInvariant();
            tag = tag switch { "strong" => "b", "em" => "i", _ => tag };
            if (tag == "br" && !closing) { Append("\n", decode: false); return; }
            if (tag is not ("b" or "i" or "u" or "font" or "span")) return;
            if (closing)
            {
                var matching = stack.FindLastIndex(item => item.Tag == tag);
                if (matching < 0) return;
                style = stack[matching].Previous;
                stack.RemoveRange(matching, stack.Count - matching);
                return;
            }
            if (stack.Count >= MaximumDepth) return;
            stack.Add((tag, style));
            style = tag switch
            {
                "b" => style with { Bold = true }, "i" => style with { Italic = true },
                "u" => style with { Underline = true }, _ => style
            };
            if (tag == "span" && Regex.IsMatch(body, "^span\\s+class\\s*=\\s*(['\"])hud-parameter\\1\\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                style = style with { Parameter = true };
            var match = (tag == "span" ? StyleAttribute : ColorAttribute).Match(body);
            if (match.Success && HudPalette.TryResolve(match.Groups["value"].Value, out var resolved))
                style = style with { Color = resolved, ExplicitColor = true };
        }
    }

    private static HudDocument Wrap(List<(Rune Glyph, HudRunStyle Style)> glyphs, int width, int maximumLines)
    {
        var lines = new List<HudRun[]>();
        var cursor = 0;
        while (cursor < glyphs.Count && lines.Count < maximumLines)
        {
            var end = cursor;
            while (end < glyphs.Count && end - cursor < width && glyphs[end].Glyph.Value != '\n') end++;
            if (end < glyphs.Count && glyphs[end].Glyph.Value != '\n' && end - cursor == width)
            {
                var space = end - 1;
                while (space > cursor + width / 2 && glyphs[space].Glyph.Value != ' ') space--;
                if (space > cursor + width / 2) end = space;
            }
            var runs = new List<HudRun>();
            for (var i = cursor; i < end; i++)
            {
                var (glyph, style) = glyphs[i];
                if (runs.Count > 0 && (runs[^1].Style == style || runs.Count == MaximumRuns))
                    runs[^1] = runs[^1] with { Text = runs[^1].Text + glyph };
                else runs.Add(new HudRun(glyph.ToString(), style));
            }
            lines.Add(runs.ToArray());
            cursor = end;
            if (cursor < glyphs.Count && glyphs[cursor].Glyph.Value == '\n') cursor++;
            else while (cursor < glyphs.Count && glyphs[cursor].Glyph.Value == ' ') cursor++;
        }
        if (cursor < glyphs.Count && lines[^1].Length > 0)
        {
            var last = lines[^1][^1];
            var runes = last.Text.EnumerateRunes().ToArray();
            lines[^1][^1] = last with { Text = string.Concat(runes.Take(Math.Max(0, runes.Length - 1))) + "…" };
        }
        return new HudDocument(lines.ToArray());
    }
}
