using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Localization.Core.Configuration;

namespace Localization.Core.Application;

internal static partial class LocalizationValidation
{
    public const int SupportedSchemaVersion = 4;
    public const int MinimumSchemaVersion = 1;

    public static readonly FrozenSet<string> CriticalKeys = new[]
    {
        "localization.menu.title",
        "localization.menu.changed",
        "localization.menu.loading",
        "localization.menu.unavailable",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> MarkupTags = new[]
    {
        "accent",
        "default",
        "warning",
        "success",
        "important",
        "muted",
        "color",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static void ValidateFallback(LocalizationFallbackConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.SchemaVersion is < MinimumSchemaVersion or > SupportedSchemaVersion)
        {
            throw new InvalidDataException(
                $"Неподдерживаемая schemaVersion локализации: {config.SchemaVersion}.");
        }

        if (config.Version <= 0)
        {
            throw new InvalidDataException("Version fallback-конфигурации должна быть больше нуля.");
        }

        if (config.GeneratedAt == default || config.GeneratedAt == DateTimeOffset.UnixEpoch)
        {
            throw new InvalidDataException("GeneratedAt fallback-конфигурации не заполнен.");
        }

        var languages = NormalizeLanguages(config.Languages);
        var colorTags = LocalizationColorSchema.FromConfig(
            config.SchemaVersion >= 3 ? config.ColorTags : null);
        var fallback = LocaleNormalizer.Normalize(config.ServerFallbackLanguage);
        if (fallback.Length == 0 || !languages.Contains(fallback))
        {
            throw new InvalidDataException("ServerFallbackLanguage должен присутствовать в languages.");
        }

        if (config.RefreshIntervalSeconds < 5)
        {
            throw new InvalidDataException("RefreshIntervalSeconds не может быть меньше 5 секунд.");
        }

        foreach (var (key, values) in config.Entries)
        {
            ValidateKey(key);
            var normalized = NormalizeTranslations(values, languages);
            if (CriticalKeys.Contains(key)
                && (!normalized.TryGetValue(fallback, out var fallbackText)
                    || string.IsNullOrWhiteSpace(fallbackText)))
            {
                throw new InvalidDataException(
                    $"Для критического ключа '{key}' отсутствует перевод fallback-языка '{fallback}'.");
            }

            var parameters = LocalizationParameterSchema.FromConfig(
                config.SchemaVersion >= 2 && config.Parameters.TryGetValue(key, out var configured)
                    ? configured
                    : null,
                normalized);
            ValidateTranslations(key, normalized, fallback, colorTags);
            _ = parameters;
        }

        foreach (var key in config.Parameters.Keys)
        {
            if (!config.Entries.ContainsKey(key))
            {
                throw new InvalidDataException(
                    $"Схема параметров ссылается на отсутствующий ключ '{key}'.");
            }
        }

        foreach (var criticalKey in CriticalKeys)
        {
            if (!config.Entries.ContainsKey(criticalKey))
            {
                throw new InvalidDataException(
                    $"В fallback-конфигурации отсутствует критический ключ '{criticalKey}'.");
            }
        }

        if (config.SchemaVersion >= 4)
        {
            ValidateFallbackTags(config.Tags, config.Entries, languages, fallback);
        }

        var expectedChecksum = FallbackConfigChecksum.Compute(config);
        if (!string.Equals(expectedChecksum, config.Checksum, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Checksum fallback-конфигурации не совпадает с содержимым.");
        }
    }

    public static void ValidateSnapshot(LocalizationSnapshot snapshot)
    {
        var fallback = snapshot.Settings.ServerFallbackLanguage;
        if (!snapshot.IsLanguageEnabled(fallback))
        {
            throw new InvalidDataException("Fallback-язык сервера отсутствует или отключён.");
        }

        var enabledLanguages = snapshot.Languages.Values
            .Where(language => language.Enabled)
            .Select(language => language.Code)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in snapshot.Entries.Values)
        {
            ValidateKey(entry.Key);
            if (!entry.Translations.ContainsKey(fallback)
                && (entry.IsCritical || CriticalKeys.Contains(entry.Key)))
            {
                throw new InvalidDataException(
                    $"Для критического ключа '{entry.Key}' отсутствует fallback-перевод '{fallback}'.");
            }

            ValidateTranslations(
                entry.Key,
                entry.Translations,
                fallback,
                snapshot.Settings.ColorTags,
                enabledLanguages);
            _ = LocalizationParameterSchema.Normalize(
                entry.Parameters.Values,
                entry.Translations
                    .Where(item => enabledLanguages.Contains(item.Key))
                    .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));
        }

        foreach (var criticalKey in CriticalKeys)
        {
            if (!snapshot.Entries.ContainsKey(criticalKey))
            {
                throw new InvalidDataException(
                    $"В snapshot отсутствует критический ключ '{criticalKey}'.");
            }
        }

        foreach (var tag in snapshot.Tags.Values)
        {
            ValidateTagKey(tag.Key);
            if (!LocalizationColorSchema.SupportedColors.Contains(tag.Color))
            {
                throw new InvalidDataException(
                    $"Тег '{tag.Key}' использует неподдерживаемый цвет '{tag.Color}'.");
            }

            var expectedLocalizationKey = $"Tags.{tag.Key}";
            if (!string.Equals(tag.LocalizationKey, expectedLocalizationKey, StringComparison.OrdinalIgnoreCase)
                || !snapshot.Entries.TryGetValue(tag.LocalizationKey, out var entry))
            {
                throw new InvalidDataException(
                    $"Для тега '{tag.Key}' отсутствует ключ локализации '{expectedLocalizationKey}'.");
            }

            ValidateTagTranslations(tag.Key, entry.Translations, fallback);
        }
    }

    private static void ValidateFallbackTags(
        IReadOnlyDictionary<string, LocalizationFallbackTagConfig> tags,
        IReadOnlyDictionary<string, Dictionary<string, string>> entries,
        IReadOnlySet<string> languages,
        string fallback)
    {
        foreach (var (tagKey, tag) in tags)
        {
            ValidateTagKey(tagKey);
            var color = tag.Color.Trim().ToLowerInvariant();
            if (!LocalizationColorSchema.SupportedColors.Contains(color))
            {
                throw new InvalidDataException(
                    $"Тег '{tagKey}' использует неподдерживаемый цвет '{tag.Color}'.");
            }

            var localizationKey = $"Tags.{tagKey}";
            if (!entries.TryGetValue(localizationKey, out var values))
            {
                throw new InvalidDataException(
                    $"Для тега '{tagKey}' отсутствует ключ локализации '{localizationKey}'.");
            }

            ValidateTagTranslations(
                tagKey,
                NormalizeTranslations(values, languages),
                fallback);
        }
    }

    private static void ValidateTagKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !TagKeyRegex().IsMatch(key))
        {
            throw new InvalidDataException(
                $"Некорректный ключ тега '{key}': ожидается строчная латиница, цифры, точка, дефис или подчёркивание.");
        }
    }

    private static void ValidateTagTranslations(
        string tagKey,
        IReadOnlyDictionary<string, string> translations,
        string fallback)
    {
        if (!translations.TryGetValue(fallback, out var fallbackText)
            || string.IsNullOrWhiteSpace(fallbackText))
        {
            throw new InvalidDataException(
                $"Для тега '{tagKey}' отсутствует перевод fallback-языка '{fallback}'.");
        }

        foreach (var (language, text) in translations)
        {
            if (text.Length > 64
                || text.IndexOfAny(['{', '}', '[', ']']) >= 0
                || ExtractPlaceholders(text).Count > 0)
            {
                throw new InvalidDataException(
                    $"Тег '{tagKey}' для языка '{language}' должен быть обычным текстом длиной до 64 символов без placeholder и разметки.");
            }
        }
    }

    public static FrozenSet<string> NormalizeLanguages(IEnumerable<string> languages)
    {
        var result = languages
            .Select(LocaleNormalizer.Normalize)
            .Where(code => code.Length > 0)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        if (result.Count == 0)
        {
            throw new InvalidDataException("Список languages не может быть пустым.");
        }

        return result;
    }

    public static FrozenDictionary<string, string> NormalizeTranslations(
        IEnumerable<KeyValuePair<string, string>> values,
        IReadOnlySet<string>? allowedLanguages = null)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rawLanguage, rawText) in values)
        {
            var language = LocaleNormalizer.Normalize(rawLanguage);
            if (language.Length == 0 || string.IsNullOrWhiteSpace(rawText))
            {
                continue;
            }

            if (allowedLanguages is not null && !allowedLanguages.Contains(language))
            {
                throw new InvalidDataException(
                    $"Перевод использует язык '{language}', которого нет в languages.");
            }

            result[language] = rawText;
        }

        return result.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !KeyRegex().IsMatch(key))
        {
            throw new InvalidDataException($"Некорректный ключ локализации '{key}'.");
        }
    }

    private static void ValidateTranslations(
        string key,
        IReadOnlyDictionary<string, string> translations,
        string fallback,
        IReadOnlyDictionary<string, string> colorTags,
        IReadOnlySet<string>? enabledLanguages = null)
    {
        foreach (var (language, text) in translations)
        {
            if (enabledLanguages is not null && !enabledLanguages.Contains(language))
            {
                continue;
            }

            if (!HasValidMarkup(text, colorTags))
            {
                throw new InvalidDataException(
                    $"Markup ключа '{key}' для языка '{language}' содержит незакрытые или вложенные неверно теги.");
            }
        }

        if (!translations.TryGetValue(fallback, out var fallbackText))
        {
            return;
        }

        var expectedPlaceholders = ExtractPlaceholders(fallbackText);
        foreach (var (language, text) in translations)
        {
            if (enabledLanguages is not null && !enabledLanguages.Contains(language))
            {
                continue;
            }

            if (!ExtractPlaceholders(text).SetEquals(expectedPlaceholders))
            {
                throw new InvalidDataException(
                    $"Placeholder ключа '{key}' для языка '{language}' не совпадают с fallback-переводом.");
            }

        }
    }

    internal static FrozenSet<string> ExtractPlaceholders(string text)
    {
        var pairedTags = ClosingMarkupRegex().Matches(text)
            .Select(match => match.Groups["name"].Value)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        return PlaceholderRegex().Matches(text)
            .Select(match => match.Groups["name"].Value)
            .Where(name => !MarkupTags.Contains(name) && !pairedTags.Contains(name))
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasValidMarkup(
        string text,
        IReadOnlyDictionary<string, string> colorTags)
    {
        var stack = new Stack<string>();
        foreach (Match match in MarkupRegex().Matches(text))
        {
            var name = match.Groups["name"].Value.ToLowerInvariant();
            var argument = match.Groups["argument"].Value;
            var recognized = string.Equals(name, "color", StringComparison.OrdinalIgnoreCase)
                             || colorTags.ContainsKey(name);
            if (!recognized)
            {
                if (match.Groups["close"].Success)
                {
                    return false;
                }

                continue;
            }

            if (match.Groups["close"].Success)
            {
                if (argument.Length > 0
                    || stack.Count == 0
                    || !string.Equals(stack.Pop(), name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            else
            {
                if ((name == "color" && !LocalizationColorSchema.SupportedColors.Contains(argument))
                    || (name != "color" && argument.Length > 0))
                {
                    return false;
                }

                stack.Push(name);
            }
        }

        return stack.Count == 0;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9_.-]{1,190}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex KeyRegex();

    [GeneratedRegex("^[a-z0-9][a-z0-9_.-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex TagKeyRegex();

    [GeneratedRegex(@"\{(?<name>[a-z][a-z0-9_]*)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(
        @"\{(?<close>/)?(?<name>[a-z][a-z0-9_]*)(?::(?<argument>[a-z]+))?\}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MarkupRegex();

    [GeneratedRegex(
        @"\{/(?<name>[a-z][a-z0-9_]*)\}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ClosingMarkupRegex();
}

internal static class FallbackConfigChecksum
{
    public static string Compute(LocalizationFallbackConfig config)
    {
        var builder = new StringBuilder();
        Append(builder, "schemaVersion", config.SchemaVersion.ToString(CultureInfo.InvariantCulture));
        Append(builder, "version", config.Version.ToString(CultureInfo.InvariantCulture));
        Append(builder, "generatedAt", config.GeneratedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        Append(builder, "serverFallbackLanguage", LocaleNormalizer.Normalize(config.ServerFallbackLanguage));
        Append(builder, "refreshIntervalSeconds", config.RefreshIntervalSeconds.ToString(CultureInfo.InvariantCulture));
        Append(builder, "localCacheEnabled", config.LocalCacheEnabled ? "1" : "0");
        Append(builder, "logMissingKeys", config.LogMissingKeys ? "1" : "0");

        foreach (var language in config.Languages.Select(LocaleNormalizer.Normalize)
                     .Where(code => code.Length > 0)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Order(StringComparer.Ordinal))
        {
            Append(builder, "language", language);
        }

        foreach (var entry in config.Entries.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            foreach (var translation in entry.Value
                         .Select(item => new KeyValuePair<string, string>(
                             LocaleNormalizer.Normalize(item.Key),
                             item.Value.Replace("\r\n", "\n", StringComparison.Ordinal)))
                         .Where(item => item.Key.Length > 0)
                         .OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                Append(builder, "entry", entry.Key);
                Append(builder, "translation", translation.Key);
                Append(builder, "text", translation.Value);
            }
        }

        if (config.SchemaVersion >= 2)
        {
            foreach (var entry in config.Entries.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                var translations = LocalizationValidation.NormalizeTranslations(entry.Value);
                var parameters = LocalizationParameterSchema.FromConfig(
                    config.Parameters.TryGetValue(entry.Key, out var configured) ? configured : null,
                    translations);
                foreach (var parameter in parameters.Values.OrderBy(
                             item => item.Name,
                             StringComparer.Ordinal))
                {
                    Append(builder, "parameterEntry", entry.Key);
                    Append(builder, "parameterName", parameter.Name);
                    Append(builder, "parameterType", LocalizationParameterSchema.ToWireType(parameter.Type));
                    Append(builder, "parameterRequired", parameter.IsRequired ? "1" : "0");
                    Append(builder, "parameterDescription", parameter.Description ?? string.Empty);
                    Append(builder, "parameterExample", parameter.Example.Replace("\r\n", "\n", StringComparison.Ordinal));
                }
            }
        }

        if (config.SchemaVersion >= 3)
        {
            var colorTags = LocalizationColorSchema.FromConfig(config.ColorTags);
            foreach (var colorTag in colorTags.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                Append(builder, "colorTagName", colorTag.Key);
                Append(builder, "colorTagColor", colorTag.Value);
            }
        }

        if (config.SchemaVersion >= 4)
        {
            foreach (var tag in config.Tags.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                Append(builder, "tagKey", tag.Key);
                Append(builder, "tagLocalizationKey", $"Tags.{tag.Key}");
                Append(builder, "tagColor", tag.Value.Color.Trim().ToLowerInvariant());
                Append(builder, "tagEnabled", tag.Value.Enabled ? "1" : "0");
                Append(builder, "tagSortOrder", tag.Value.SortOrder.ToString(CultureInfo.InvariantCulture));
            }
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static void Append(StringBuilder builder, string name, string value)
    {
        builder.Append(Encoding.UTF8.GetByteCount(name)).Append(':').Append(name)
            .Append(Encoding.UTF8.GetByteCount(value)).Append(':').Append(value).Append('\n');
    }
}
