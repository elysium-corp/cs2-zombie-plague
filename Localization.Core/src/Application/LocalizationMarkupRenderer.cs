using System.Text;
using System.Text.RegularExpressions;

namespace Localization.Core.Application;

internal static partial class LocalizationMarkupRenderer
{
    public static string Render(string text, IReadOnlyDictionary<string, string> colorTags)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var output = new StringBuilder(text.Length + 24);
        var stack = new List<(string Name, string Color)>
        {
            ("root", colorTags.TryGetValue("default", out var defaultColor) ? defaultColor : "default"),
        };
        var position = 0;
        var rendered = false;

        foreach (Match match in MarkupRegex().Matches(text))
        {
            var name = match.Groups["name"].Value.ToLowerInvariant();
            var argument = match.Groups["argument"].Value.ToLowerInvariant();
            var isDirectColor = string.Equals(name, "color", StringComparison.OrdinalIgnoreCase)
                                && (match.Groups["close"].Success
                                    ? argument.Length == 0
                                    : LocalizationColorSchema.SupportedColors.Contains(argument));
            if (!isDirectColor && (!colorTags.TryGetValue(name, out _) || argument.Length > 0))
            {
                continue;
            }

            output.Append(text, position, match.Index - position);
            position = match.Index + match.Length;
            rendered = true;

            if (match.Groups["close"].Success)
            {
                if (stack.Count > 1
                    && string.Equals(stack[^1].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    stack.RemoveAt(stack.Count - 1);
                    output.Append('[').Append(stack[^1].Color).Append(']');
                }
                else
                {
                    output.Append(match.Value);
                }
                continue;
            }

            var color = isDirectColor ? argument : colorTags[name];
            stack.Add((name, color));
            output.Append('[').Append(color).Append(']');
        }

        if (!rendered)
        {
            return text;
        }

        output.Append(text, position, text.Length - position).Append("[/]");
        return output.ToString();
    }

    [GeneratedRegex(
        @"\{(?<close>/)?(?<name>[a-z][a-z0-9_]*)(?::(?<argument>[a-z]+))?\}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MarkupRegex();
}
