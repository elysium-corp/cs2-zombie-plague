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
