using Localization.Api;

namespace Localization.Core.Data;

internal enum LocalizationSource
{
    Database,
    Config,
}

internal sealed record LocalizationSettings(
    string ServerFallbackLanguage,
    int RefreshIntervalSeconds,
    bool LogMissingKeys,
    long ConfigurationVersion,
    FrozenDictionary<string, string> ColorTags);

internal sealed record LocalizationLanguageState(
    long Id,
    string Code,
    string Name,
    string NativeName,
    bool Enabled,
    int SortOrder)
{
    public LocalizationLanguage ToContract() => new(Code, Name, NativeName, SortOrder);
}

internal sealed record LocalizationEntry(
    long Id,
    string Key,
    bool IsCritical,
    FrozenDictionary<string, string> Translations,
    FrozenDictionary<string, LocalizationParameterDefinition> Parameters);

internal sealed record LocalizationTagState(
    long Id,
    string Key,
    string LocalizationKey,
    string Color,
    bool Enabled,
    int SortOrder);

internal sealed record LocalizationSnapshot(
    LocalizationSettings Settings,
    FrozenDictionary<string, LocalizationLanguageState> Languages,
    FrozenDictionary<string, LocalizationEntry> Entries,
    FrozenDictionary<string, LocalizationTagState> Tags,
    DateTimeOffset LoadedAt,
    LocalizationSource Source)
{
    public bool IsLanguageEnabled(string? code)
    {
        var normalized = LocaleNormalizer.Normalize(code);
        return normalized.Length > 0
               && Languages.TryGetValue(normalized, out var language)
               && language.Enabled;
    }
}

internal sealed class LocalizationCache
{
    private LocalizationSnapshot? _current;

    public LocalizationSnapshot? Current => Volatile.Read(ref _current);

    public void Replace(LocalizationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Interlocked.Exchange(ref _current, snapshot);
    }
}

internal static class LocaleNormalizer
{
    public static string Normalize(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return string.Empty;
        }

        var value = locale.Trim().Replace('_', '-');
        return value.ToLowerInvariant() switch
        {
            "russian" or "ru-ru" => "ru",
            "english" or "en-us" or "en-gb" => "en",
            "ukrainian" or "uk-ua" => "uk",
            "polish" or "pl-pl" => "pl",
            "german" or "de-de" => "de",
            "portuguese" or "pt-br" => "pt-BR",
            "schinese" or "zh-cn" => "zh-CN",
            "tchinese" or "zh-tw" => "zh-TW",
            _ when value.Length > 2 && value[2] == '-' => value[..2].ToLowerInvariant(),
            _ => value.ToLowerInvariant(),
        };
    }
}
