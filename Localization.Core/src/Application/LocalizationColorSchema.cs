using System.Text.Json;
using System.Text.RegularExpressions;

namespace Localization.Core.Application;

internal static partial class LocalizationColorSchema
{
    public const int MaximumTagCount = 64;

    public static readonly FrozenSet<string> SupportedColors = new[]
    {
        "default",
        "white",
        "darkred",
        "lightpurple",
        "green",
        "olive",
        "lime",
        "red",
        "gray",
        "grey",
        "lightyellow",
        "yellow",
        "silver",
        "bluegrey",
        "lightblue",
        "blue",
        "darkblue",
        "purple",
        "magenta",
        "lightred",
        "gold",
        "orange",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenDictionary<string, string> Defaults =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = "default",
            ["accent"] = "lightblue",
            ["warning"] = "red",
            ["success"] = "green",
            ["important"] = "orange",
            ["muted"] = "gray",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static FrozenDictionary<string, string> FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Defaults;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Normalize(values);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("JSON цветовых тегов локализации повреждён.", exception);
        }
    }

    public static FrozenDictionary<string, string> FromConfig(
        IEnumerable<KeyValuePair<string, string>>? configured)
    {
        return Normalize(configured);
    }

    private static FrozenDictionary<string, string> Normalize(
        IEnumerable<KeyValuePair<string, string>>? configured)
    {
        var result = Defaults.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        if (configured is null)
        {
            return result.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        }

        var values = configured.ToArray();
        if (values.Length > MaximumTagCount)
        {
            throw new InvalidDataException(
                $"Количество цветовых тегов не может превышать {MaximumTagCount}.");
        }

        var configuredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rawName, rawColor) in values)
        {
            var name = rawName?.Trim().ToLowerInvariant() ?? string.Empty;
            var color = rawColor?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!TagNameRegex().IsMatch(name))
            {
                throw new InvalidDataException($"Некорректное имя цветового тега '{rawName}'.");
            }

            if (string.Equals(name, "color", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Имя цветового тега 'color' зарезервировано.");
            }

            if (!configuredNames.Add(name))
            {
                throw new InvalidDataException($"Цветовой тег '{name}' указан несколько раз.");
            }

            if (!SupportedColors.Contains(color))
            {
                throw new InvalidDataException(
                    $"Цветовой тег '{name}' использует неподдерживаемый цвет '{rawColor}'.");
            }

            result[name] = color;
            if (result.Count > MaximumTagCount)
            {
                throw new InvalidDataException(
                    $"Общее количество цветовых тегов не может превышать {MaximumTagCount}.");
            }
        }

        return result.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex TagNameRegex();
}
