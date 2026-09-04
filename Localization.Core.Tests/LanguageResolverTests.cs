using Localization.Core.Application;
using Localization.Core.Data;

namespace Localization.Core.Tests;

public sealed class LanguageResolverTests
{
    [Fact]
    public void ManualPreferenceExists_ReturnsManualPreference()
    {
        var language = LanguageResolver.Resolve("en", "ru", CreateSnapshot());

        Assert.Equal("en", language);
    }

    [Fact]
    public void ManualPreferenceMissing_ClientLanguageSupported_ReturnsClientLanguage()
    {
        var language = LanguageResolver.Resolve(null, "english", CreateSnapshot());

        Assert.Equal("en", language);
    }

    [Fact]
    public void ManualPreferenceMissing_ClientLanguageUnsupported_ReturnsServerFallback()
    {
        var language = LanguageResolver.Resolve(null, "fr", CreateSnapshot());

        Assert.Equal("ru", language);
    }

    [Fact]
    public void ManualPreferenceDisabled_ClientLanguageSupported_ReturnsClientLanguage()
    {
        var language = LanguageResolver.Resolve("de", "en", CreateSnapshot(disabled: "de"));

        Assert.Equal("en", language);
    }

    [Fact]
    public void ManualPreferenceDisabled_ClientLanguageUnsupported_ReturnsServerFallback()
    {
        var language = LanguageResolver.Resolve("de", "fr", CreateSnapshot(disabled: "de"));

        Assert.Equal("ru", language);
    }

    private static LocalizationSnapshot CreateSnapshot(string? disabled = null)
    {
        var languages = new[]
        {
            new LocalizationLanguageState(1, "ru", "Русский", "Русский", disabled != "ru", 10),
            new LocalizationLanguageState(2, "en", "English", "English", disabled != "en", 20),
            new LocalizationLanguageState(3, "de", "Deutsch", "Deutsch", disabled != "de", 30),
        }.ToFrozenDictionary(language => language.Code, StringComparer.OrdinalIgnoreCase);

        return new LocalizationSnapshot(
            new LocalizationSettings("ru", 30, true, 1, LocalizationColorSchema.Defaults),
            languages,
            FrozenDictionary<string, LocalizationEntry>.Empty,
            FrozenDictionary<string, LocalizationTagState>.Empty,
            DateTimeOffset.UtcNow,
            LocalizationSource.Database);
    }
}
