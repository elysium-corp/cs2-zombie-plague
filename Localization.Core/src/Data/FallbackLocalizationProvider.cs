using System.Globalization;
using Localization.Core.Application;
using Localization.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Localization.Core.Data;

internal sealed class FallbackLocalizationProvider(IOptionsMonitor<LocalizationFallbackConfig> options)
{
    public LocalizationSnapshot Load() => Load(options.CurrentValue);

    internal static LocalizationSnapshot Load(LocalizationFallbackConfig config)
    {
        LocalizationValidation.ValidateFallback(config);
        return Build(config, LocalizationSource.Config);
    }

    internal static LocalizationSnapshot Build(
        LocalizationFallbackConfig config,
        LocalizationSource source)
    {
        var orderedLanguageCodes = config.Languages
            .Select(LocaleNormalizer.Normalize)
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var languageCodes = LocalizationValidation.NormalizeLanguages(orderedLanguageCodes);
        var colorTags = LocalizationColorSchema.FromConfig(
            config.SchemaVersion >= 3 ? config.ColorTags : null);
        var languages = orderedLanguageCodes
            .Select((code, index) => CreateLanguageState(-(index + 1L), code, index))
            .ToFrozenDictionary(language => language.Code, StringComparer.OrdinalIgnoreCase);

        long entryId = -1;
        var entries = config.Entries
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                item => item.Key,
                item =>
                {
                    var translations = LocalizationValidation.NormalizeTranslations(
                        item.Value,
                        languageCodes);
                    var parameters = LocalizationParameterSchema.FromConfig(
                        config.SchemaVersion >= 2
                        && config.Parameters.TryGetValue(item.Key, out var configured)
                            ? configured
                            : null,
                        translations);
                    return new LocalizationEntry(
                        entryId--,
                        item.Key,
                        LocalizationValidation.CriticalKeys.Contains(item.Key),
                        translations,
                        parameters);
                },
                StringComparer.OrdinalIgnoreCase);
        long tagId = -1;
        var tags = (config.SchemaVersion >= 4
                ? config.Tags
                : new Dictionary<string, LocalizationFallbackTagConfig>(StringComparer.OrdinalIgnoreCase))
            .OrderBy(item => item.Value.SortOrder)
            .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                item => item.Key,
                item => new LocalizationTagState(
                    tagId--,
                    item.Key,
                    $"Tags.{item.Key}",
                    item.Value.Color.Trim().ToLowerInvariant(),
                    item.Value.Enabled,
                    item.Value.SortOrder),
                StringComparer.OrdinalIgnoreCase);

        var snapshot = new LocalizationSnapshot(
            new LocalizationSettings(
                LocaleNormalizer.Normalize(config.ServerFallbackLanguage),
                Math.Max(5, config.RefreshIntervalSeconds),
                config.LogMissingKeys,
                config.Version,
                colorTags),
            languages,
            entries,
            tags,
            DateTimeOffset.UtcNow,
            source);

        LocalizationValidation.ValidateSnapshot(snapshot);
        return snapshot;
    }

    private static LocalizationLanguageState CreateLanguageState(long id, string code, int sortOrder)
    {
        var normalized = LocaleNormalizer.Normalize(code);
        try
        {
            var culture = CultureInfo.GetCultureInfo(normalized);
            return new LocalizationLanguageState(
                id,
                normalized,
                culture.EnglishName,
                culture.NativeName,
                true,
                sortOrder);
        }
        catch (CultureNotFoundException)
        {
            var displayName = normalized.ToUpperInvariant();
            return new LocalizationLanguageState(
                id,
                normalized,
                displayName,
                displayName,
                true,
                sortOrder);
        }
    }
}
