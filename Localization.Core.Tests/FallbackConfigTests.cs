using Localization.Core.Application;
using Localization.Core.Configuration;
using Localization.Core.Data;

namespace Localization.Core.Tests;

public sealed class FallbackConfigTests
{
    [Fact]
    public void ValidConfig_PassesChecksumPlaceholderAndMarkupValidation()
    {
        var config = CreateConfig();
        config.Checksum = FallbackConfigChecksum.Compute(config);

        LocalizationValidation.ValidateFallback(config);
    }

    [Fact]
    public void FallbackLanguageMissingFromLanguages_IsRejected()
    {
        var config = CreateConfig();
        config.ServerFallbackLanguage = "fr";
        config.Checksum = FallbackConfigChecksum.Compute(config);

        Assert.Throws<InvalidDataException>(() => LocalizationValidation.ValidateFallback(config));
    }

    [Fact]
    public void DifferentPlaceholders_AreRejected()
    {
        var config = CreateConfig();
        config.Entries["localization.menu.changed"]["en"] = "Language changed to {locale}";
        config.Checksum = FallbackConfigChecksum.Compute(config);

        Assert.Throws<InvalidDataException>(() => LocalizationValidation.ValidateFallback(config));
    }

    [Fact]
    public void NonCriticalEntryWithoutFallbackTranslation_IsAllowed()
    {
        var config = CreateConfig();
        config.Entries["optional.only_english"] = new Dictionary<string, string>
        {
            ["en"] = "Optional text",
        };
        config.Checksum = FallbackConfigChecksum.Compute(config);

        LocalizationValidation.ValidateFallback(config);
    }

    [Fact]
    public void Build_PreservesConfiguredLanguageOrder()
    {
        var config = CreateConfig();
        config.Languages = ["pl", "de", "en", "ru"];

        var snapshot = FallbackLocalizationProvider.Build(config, LocalizationSource.Config);
        var languages = snapshot.Languages.Values
            .OrderBy(language => language.SortOrder)
            .Select(language => language.Code)
            .ToArray();

        Assert.Equal(config.Languages, languages);
    }

    [Fact]
    public void SnapshotValidation_IgnoresStaleTranslationOfDisabledLanguage()
    {
        var snapshot = FallbackLocalizationProvider.Build(CreateConfig(), LocalizationSource.Config);
        var languages = snapshot.Languages.Values
            .Select(language => language.Code == "de" ? language with { Enabled = false } : language)
            .ToFrozenDictionary(language => language.Code, StringComparer.OrdinalIgnoreCase);
        var translations = snapshot.Entries["localization.menu.changed"].Translations
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        translations["de"] = "Sprache geändert zu {locale}";
        var changedEntry = snapshot.Entries["localization.menu.changed"] with
        {
            Translations = translations.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
        };
        var entries = snapshot.Entries.Values
            .Select(entry => entry.Key == changedEntry.Key ? changedEntry : entry)
            .ToFrozenDictionary(entry => entry.Key, StringComparer.OrdinalIgnoreCase);

        Assert.Throws<InvalidDataException>(() =>
            LocalizationValidation.ValidateSnapshot(snapshot with { Entries = entries }));
        LocalizationValidation.ValidateSnapshot(snapshot with { Languages = languages, Entries = entries });
    }

    private static LocalizationFallbackConfig CreateConfig()
    {
        return new LocalizationFallbackConfig
        {
            SchemaVersion = 1,
            Version = 1,
            GeneratedAt = new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero),
            ServerFallbackLanguage = "ru",
            Languages = ["ru", "en", "de", "pl"],
            Entries = BuiltInLocalizationEntries.Create(),
        };
    }
}
