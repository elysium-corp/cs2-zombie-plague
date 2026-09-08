using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Localization.Api;

namespace Localization.Core.Application;

internal static class LocalizationHtmlMarkup
{
    private static readonly Regex Tokens = new(@"<[^>]*>|\[(?:/?|[a-z]+)\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Tag = new(@"^<(?<close>/)?(?<name>b|strong|i|em|u|br|font|span)(?<attrs>\s[^>]*|)>$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Font = new("^\\s+color\\s*=\\s*(['\"])(?<color>#[a-f0-9]{3}(?:[a-f0-9]{3})?|[a-z]+)\\1\\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Span = new("^\\s+style\\s*=\\s*(['\"])\\s*color\\s*:\\s*(?<color>#[a-f0-9]{3}(?:[a-f0-9]{3})?|[a-z]+)\\s*;?\\s*\\1\\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Parameter = new("^\\s+class\\s*=\\s*(['\"])hud-parameter\\1\\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly IReadOnlyDictionary<string, int> Colors = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["white"] = 0xffffff, ["default"] = 0xffffff, ["black"] = 0, ["red"] = 0xff5555,
        ["darkred"] = 0xcc3333, ["green"] = 0x55dd88, ["lightgreen"] = 0x99ff99, ["lime"] = 0xbfff55,
        ["blue"] = 0x5599ff, ["lightblue"] = 0x99ccff, ["cyan"] = 0x55ddff, ["yellow"] = 0xffdd55,
        ["gold"] = 0xeac16a, ["orange"] = 0xff9955, ["purple"] = 0xaa77ff, ["lightpurple"] = 0xccaaff,
        ["pink"] = 0xff88cc, ["gray"] = 0xaaaaaa, ["grey"] = 0xaaaaaa, ["silver"] = 0xcccccc,
        ["mint"] = 0x85dcb1, ["muted"] = 0x9cafb5, ["olive"] = 0xa5af62, ["lightyellow"] = 0xfff2a6,
        ["bluegrey"] = 0x8aaac2, ["darkblue"] = 0x3366cc, ["magenta"] = 0xff55ff, ["lightred"] = 0xff8888,
    };

    internal static string Escape(string text) => WebUtility.HtmlEncode(text).Replace("[", "&#91;").Replace("]", "&#93;");

    internal static bool IsValid(string text)
    {
        var stack = new Stack<string>();
        foreach (Match token in Tokens.Matches(text))
        {
            if (token.Value[0] != '<') continue;
            if (!Read(token.Value, out var name, out var closing, out _, out _)) return false;
            if (name == "br") continue;
            if (closing)
            {
                if (stack.Count == 0 || stack.Pop() != name) return false;
            }
            else
            {
                if (stack.Count >= 16) return false;
                stack.Push(name);
            }
        }
        return stack.Count == 0;
    }

    internal static string Render(string text, LocalizationOutputMode mode, LocalizationPlayerStyle role)
    {
        if (mode == LocalizationOutputMode.Raw) return text;
        if (mode == LocalizationOutputMode.Html) return RenderHtml(text, role);
        var html = false;
        var output = new StringBuilder(text.Length + 64);
        var stack = new List<(string Name, string Close, string Color)>();
        var current = "default";
        var position = 0;
        foreach (Match token in Tokens.Matches(text))
        {
            Append(text[position..token.Index]);
            position = token.Index + token.Length;
            if (token.Value[0] == '[')
            {
                if (html) { Append(token.Value); continue; }
                var code = token.Value[1..^1];
                if (code is "" or "/") current = "default";
                else if (LocalizationColorSchema.SupportedColors.Contains(code)) current = code;
                output.Append(token.Value);
                continue;
            }
            if (!Read(token.Value, out var name, out var closing, out var color, out var parameter)) continue;
            if (name == "br") { output.Append(html ? "<br>" : "\n"); continue; }
            if (closing)
            {
                if (stack.Count == 0 || stack[^1].Name != name) continue;
                var previous = stack[^1];
                stack.RemoveAt(stack.Count - 1);
                if (html) output.Append(previous.Close);
                else if (previous.Close.Length > 0) output.Append('[').Append(previous.Color).Append(']');
                current = previous.Color;
                continue;
            }
            if (stack.Count >= 16) continue;
            var close = string.Empty;
            if (color is not null)
            {
                if (color == "role") color = html ? role.HudColor : role.ChatColor;
                if (html) { output.Append("<font color=\"").Append(color).Append("\">"); close = "</font>"; }
                else { close = "color"; output.Append('[').Append(ChatColor(color)).Append(']'); }
            }
            else if (html)
            {
                output.Append(parameter ? "<span class=\"hud-parameter\">" : $"<{name}>");
                close = $"</{name}>";
            }
            stack.Add((name, close, current));
            if (color is not null) current = ChatColor(color);
        }
        Append(text[position..]);
        if (html) foreach (var item in stack.AsEnumerable().Reverse()) output.Append(item.Close);
        else if (stack.Any(x => x.Close.Length > 0)) output.Append("[default]");
        return output.ToString();

        void Append(string value) => output.Append(html ? Escape(WebUtility.HtmlDecode(value)) : WebUtility.HtmlDecode(value));
    }

    private readonly record struct HtmlStyle(string? Color = null, bool Bold = false, bool Italic = false, bool Underline = false, bool Parameter = false);

    // Каждый фрагмент получает сбалансированную разметку: старые чатовые коды не ломают вложенные HTML-теги.
    private static string RenderHtml(string text, LocalizationPlayerStyle role)
    {
        var output = new StringBuilder(text.Length + 64);
        var style = new HtmlStyle();
        var stack = new List<(string Name, HtmlStyle Previous)>();
        var position = 0;
        foreach (Match token in Tokens.Matches(text))
        {
            Append(text[position..token.Index]); position = token.Index + token.Length;
            if (token.Value[0] == '[')
            {
                var code = token.Value[1..^1].ToLowerInvariant();
                if (code is "" or "/") style = style with { Color = null };
                else if (LocalizationColorSchema.SupportedColors.Contains(code)) style = style with { Color = code };
                else Append(token.Value);
                continue;
            }
            if (!Read(token.Value, out var name, out var closing, out var color, out var parameter)) continue;
            if (name == "br") { output.Append("<br>"); continue; }
            if (closing)
            {
                if (stack.Count > 0 && stack[^1].Name == name) { style = stack[^1].Previous; stack.RemoveAt(stack.Count - 1); }
                continue;
            }
            if (stack.Count >= 16) continue;
            stack.Add((name, style));
            style = style with
            {
                Color = color == "role" ? role.HudColor : color ?? style.Color,
                Bold = style.Bold || name is "b" or "strong", Italic = style.Italic || name is "i" or "em",
                Underline = style.Underline || name == "u", Parameter = style.Parameter || parameter
            };
        }
        Append(text[position..]);
        return output.ToString();

        void Append(string value)
        {
            if (value.Length == 0) return;
            if (style.Color is { } color) output.Append("<font color=\"").Append(color).Append("\">");
            if (style.Bold) output.Append("<b>");
            if (style.Italic) output.Append("<i>");
            if (style.Underline) output.Append("<u>");
            if (style.Parameter) output.Append("<span class=\"hud-parameter\">");
            output.Append(Escape(WebUtility.HtmlDecode(value)));
            if (style.Parameter) output.Append("</span>");
            if (style.Underline) output.Append("</u>");
            if (style.Italic) output.Append("</i>");
            if (style.Bold) output.Append("</b>");
            if (style.Color is not null) output.Append("</font>");
        }
    }

    private static bool Read(string token, out string name, out bool closing, out string? color, out bool parameter)
    {
        name = ""; closing = false; color = null; parameter = false;
        if (token.Length > 162) return false;
        if (Regex.IsMatch(token, @"^<br\s*/?\s*>$", RegexOptions.IgnoreCase)) { name = "br"; return true; }
        var match = Tag.Match(token);
        if (!match.Success) return false;
        name = match.Groups["name"].Value.ToLowerInvariant();
        closing = match.Groups["close"].Success;
        var attrs = match.Groups["attrs"].Value;
        if (closing) return name != "br" && string.IsNullOrWhiteSpace(attrs);
        if (name is "font" or "span")
        {
            if (name == "span" && Parameter.IsMatch(attrs)) { parameter = true; return true; }
            var attribute = (name == "font" ? Font : Span).Match(attrs);
            if (!attribute.Success) return false;
            color = attribute.Groups["color"].Value.ToLowerInvariant();
            return color == "role" || Colors.ContainsKey(color) || color.StartsWith('#');
        }
        return string.IsNullOrWhiteSpace(attrs);
    }

    private static string ChatColor(string color)
    {
        if (LocalizationColorSchema.SupportedColors.Contains(color)) return color;
        if (!Colors.TryGetValue(color, out var rgb))
        {
            var hex = color.TrimStart('#');
            if (hex.Length == 3) hex = string.Concat(hex.SelectMany(c => new[] { c, c }));
            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb)) return "default";
        }
        return Colors.Where(x => LocalizationColorSchema.SupportedColors.Contains(x.Key))
            .OrderBy(x => Distance(rgb, x.Value)).First().Key;
    }

    private static int Distance(int a, int b)
    {
        var r = (a >> 16) - (b >> 16);
        var g = (a >> 8 & 255) - (b >> 8 & 255);
        var blue = (a & 255) - (b & 255);
        return r * r + g * g + blue * blue;
    }
}
