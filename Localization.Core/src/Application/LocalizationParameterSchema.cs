using System.Globalization;
using System.Text.RegularExpressions;
using Localization.Api;
using Localization.Core.Configuration;

namespace Localization.Core.Application;

internal static partial class LocalizationParameterSchema
{
    private static readonly FrozenDictionary<string, LocalizationParameterDefinition> Empty =
        new Dictionary<string, LocalizationParameterDefinition>(StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static FrozenDictionary<string, LocalizationParameterDefinition> FromConfig(
        IEnumerable<LocalizationFallbackParameterConfig>? parameters,
        IReadOnlyDictionary<string, string> translations)
    {
        return Normalize(
            parameters?.Select(parameter => new LocalizationParameterDefinition(
                parameter.Name ?? string.Empty,
                ParseType(parameter.Type),
                parameter.Required,
                parameter.Description,
                parameter.Example ?? string.Empty)),
            translations);
    }

    public static FrozenDictionary<string, LocalizationParameterDefinition> FromJson(
        string? json,
        IReadOnlyDictionary<string, string> translations)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Normalize(null, translations);
        }

        try
        {
            var parameters = JsonSerializer.Deserialize<List<LocalizationFallbackParameterConfig>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return FromConfig(parameters, translations);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("JSON схемы параметров локализации повреждён.", exception);
        }
    }

    public static FrozenDictionary<string, LocalizationParameterDefinition> Normalize(
        IEnumerable<LocalizationParameterDefinition>? parameters,
        IReadOnlyDictionary<string, string> translations)
    {
        var placeholders = translations.Values
            .SelectMany(LocalizationValidation.ExtractPlaceholders)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        foreach (var text in translations.Values)
        {
            if (!LocalizationValidation.ExtractPlaceholders(text).SetEquals(placeholders))
            {
                throw new InvalidDataException(
                    "Placeholder должны совпадать во всех переводах ключа локализации.");
            }
        }

        var supplied = parameters?.ToArray() ?? [];

        if (supplied.Length == 0)
        {
            return placeholders.Count == 0
                ? Empty
                : placeholders
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .Select(name => new LocalizationParameterDefinition(
                        name,
                        LocalizationParameterType.String,
                        true,
                        null,
                        name))
                    .ToFrozenDictionary(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, LocalizationParameterDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in supplied)
        {
            var name = parameter.Name?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!NameRegex().IsMatch(name))
            {
                throw new InvalidDataException(
                    $"Некорректное имя параметра локализации '{parameter.Name}'.");
            }

            var description = string.IsNullOrWhiteSpace(parameter.Description)
                ? null
                : parameter.Description.Trim();
            if (description?.Length > 512)
            {
                throw new InvalidDataException(
                    $"Описание параметра '{name}' не должно превышать 512 символов.");
            }

            var example = parameter.Example?.Trim() ?? string.Empty;
            if (example.Length == 0 || !TryFormatValue(parameter.Type, example, out _))
            {
                throw new InvalidDataException(
                    $"Пример параметра '{name}' не соответствует типу {ToWireType(parameter.Type)}.");
            }

            if (!result.TryAdd(
                    name,
                    parameter with { Name = name, Description = description, Example = example }))
            {
                throw new InvalidDataException($"Параметр локализации '{name}' указан несколько раз.");
            }
        }

        if (translations.Count > 0
            && !result.Keys.ToFrozenSet(StringComparer.OrdinalIgnoreCase).SetEquals(placeholders))
        {
            throw new InvalidDataException(
                "Схема параметров локализации не совпадает с placeholder в переводах.");
        }

        return result.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryFormatValue(
        LocalizationParameterType type,
        object? value,
        out string formatted)
    {
        formatted = string.Empty;
        if (value is null)
        {
            return false;
        }

        switch (type)
        {
            case LocalizationParameterType.String:
                if (value is not string text)
                {
                    return false;
                }

                formatted = text;
                return true;
            case LocalizationParameterType.Integer:
                return TryFormatInteger(value, out formatted);
            case LocalizationParameterType.Number:
                return TryFormatNumber(value, out formatted);
            case LocalizationParameterType.Boolean:
                return TryFormatBoolean(value, out formatted);
            default:
                return false;
        }
    }

    public static LocalizationParameterType ParseType(string? type)
    {
        return type?.Trim().ToLowerInvariant() switch
        {
            "string" => LocalizationParameterType.String,
            "integer" => LocalizationParameterType.Integer,
            "number" => LocalizationParameterType.Number,
            "boolean" => LocalizationParameterType.Boolean,
            _ => throw new InvalidDataException($"Неподдерживаемый тип параметра локализации '{type}'."),
        };
    }

    public static string ToWireType(LocalizationParameterType type) => type switch
    {
        LocalizationParameterType.String => "string",
        LocalizationParameterType.Integer => "integer",
        LocalizationParameterType.Number => "number",
        LocalizationParameterType.Boolean => "boolean",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    private static bool TryFormatInteger(object value, out string formatted)
    {
        formatted = string.Empty;
        if (value is string text)
        {
            if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return false;
            }

            formatted = parsed.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        try
        {
            if (value is byte or sbyte or short or ushort or int or uint or long)
            {
                formatted = Convert.ToInt64(value, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture);
                return true;
            }

            if (value is ulong unsigned && unsigned <= long.MaxValue)
            {
                formatted = unsigned.ToString(CultureInfo.InvariantCulture);
                return true;
            }
        }
        catch (OverflowException)
        {
            return false;
        }

        return false;
    }

    private static bool TryFormatNumber(object value, out string formatted)
    {
        formatted = string.Empty;
        if (value is decimal decimalValue)
        {
            formatted = decimalValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (value is string text)
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                || !double.IsFinite(parsed))
            {
                return false;
            }

            formatted = parsed.ToString("G17", CultureInfo.InvariantCulture);
            return true;
        }

        if (value is byte or sbyte or short or ushort or int or uint or long or ulong)
        {
            formatted = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return true;
        }

        if (value is float single && float.IsFinite(single))
        {
            formatted = single.ToString("G9", CultureInfo.InvariantCulture);
            return true;
        }

        if (value is double doubleValue && double.IsFinite(doubleValue))
        {
            formatted = doubleValue.ToString("G17", CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }

    private static bool TryFormatBoolean(object value, out string formatted)
    {
        if (value is bool boolean)
        {
            formatted = boolean ? "true" : "false";
            return true;
        }

        if (value is string text && bool.TryParse(text, out boolean))
        {
            formatted = boolean ? "true" : "false";
            return true;
        }

        formatted = string.Empty;
        return false;
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{0,63}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NameRegex();
}
