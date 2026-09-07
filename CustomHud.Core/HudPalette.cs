using System.Globalization;

namespace CustomHud.Core;

internal static class HudPalette
{
    // Фиксированная палитра удерживает число сетевых CSS-классов ниже лимита движка
    internal static readonly (string Name, int Rgb)[] Named =
    [
        ("white", 0xFFFFFF), ("black", 0x000000), ("red", 0xFF5555), ("darkred", 0xCC3333),
        ("green", 0x55DD88), ("lightgreen", 0x99FF99), ("lime", 0xBFFF55),
        ("blue", 0x5599FF), ("lightblue", 0x99CCFF), ("cyan", 0x55DDFF),
        ("yellow", 0xFFDD55), ("gold", 0xEAC16A), ("orange", 0xFF9955),
        ("purple", 0xAA77FF), ("lightpurple", 0xCCAAFF), ("pink", 0xFF88CC),
        ("gray", 0xAAAAAA), ("silver", 0xCCCCCC), ("mint", 0x85DCB1), ("muted", 0x9CAFB5)
    ];

    internal static readonly int[] Colors = Enumerable.Range(0, 216)
        .Select(index => ((index / 36 * 51) << 16) | ((index / 6 % 6 * 51) << 8) | (index % 6 * 51))
        .Concat(Named.Select(color => color.Rgb)).Distinct().ToArray();

    internal static readonly int White = Array.IndexOf(Colors, 0xFFFFFF);

    internal static bool TryResolve(string value, out int index)
    {
        value = value.Trim().ToLowerInvariant();
        value = value switch { "grey" => "gray", "default" => "white", _ => value };
        var rgb = -1;
        foreach (var named in Named)
            if (named.Name == value) { rgb = named.Rgb; break; }
        if (rgb < 0 && value.StartsWith('#'))
        {
            var hex = value[1..];
            if (hex.Length == 3) hex = string.Concat(hex.SelectMany(c => new[] { c, c }));
            if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out rgb))
                rgb = -1;
        }
        index = White;
        if (rgb < 0) return false;
        var distance = int.MaxValue;
        for (var candidate = 0; candidate < Colors.Length; candidate++)
        {
            var color = Colors[candidate];
            var r = (rgb >> 16) - (color >> 16);
            var g = ((rgb >> 8) & 255) - ((color >> 8) & 255);
            var b = (rgb & 255) - (color & 255);
            var current = r * r + g * g + b * b;
            if (current >= distance) continue;
            distance = current;
            index = candidate;
            if (current == 0) break;
        }
        return true;
    }
}
