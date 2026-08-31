using Localization.Core.Application;
using Localization.Core.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace Localization.Core.Tests;

public sealed class LocalizationRuntimeTests
{
    [Fact]
    public void SelectedLanguageTranslationMissing_ReturnsServerFallbackTranslation()
    {
        var cache = new LocalizationCache();
        cache.Replace(CreateSnapshot());
        var languageResolver = new LanguageResolver(cache, new PlayerLanguageCache());
        var runtime = new LocalizationRuntime(
            cache,
            languageResolver,
            new RateLimitedLocalizationLogger(NullLogger.Instance));

        var text = runtime.GetForLanguage("de", "economy.errors.insufficient_money", null);

        Assert.Equal("Недостаточно средств", text);
    }

    [Fact]
    public void Placeholders_AreReplacedWithoutTouchingMarkup()
    {
        var cache = new LocalizationCache();
        cache.Replace(CreateSnapshot());
        var runtime = new LocalizationRuntime(
            cache,
            new LanguageResolver(cache, new PlayerLanguageCache()),
            new RateLimitedLocalizationLogger(NullLogger.Instance));

        var text = runtime.GetForLanguage(
            "en",
            "test.reward",
            new Dictionary<string, string> { ["points"] = "15" });

        Assert.Equal("{success}+15{/success}", text);
    }

    private static LocalizationSnapshot CreateSnapshot()
    {
        var languages = new[]
        {
            new LocalizationLanguageState(1, "ru", "Русский", "Русский", true, 10),
            new LocalizationLanguageState(2, "en", "English", "English", true, 20),
            new LocalizationLanguageState(3, "de", "Deutsch", "Deutsch", true, 30),
        }.ToFrozenDictionary(language => language.Code, StringComparer.OrdinalIgnoreCase);
        var entries = new[]
        {
            new LocalizationEntry(
                1,
                "economy.errors.insufficient_money",
                false,
                new Dictionary<string, string>
                {
                    ["ru"] = "Недостаточно средств",
                    ["en"] = "Not enough money",
                }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)),
            new LocalizationEntry(
                2,
                "test.reward",
                false,
                new Dictionary<string, string>
                {
                    ["ru"] = "{success}+{points}{/success}",
                    ["en"] = "{success}+{points}{/success}",
                }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)),
        }.ToFrozenDictionary(entry => entry.Key, StringComparer.OrdinalIgnoreCase);

        return new LocalizationSnapshot(
            new LocalizationSettings("ru", 30, true, true, 1),
            languages,
            entries,
            DateTimeOffset.UtcNow,
            LocalizationSource.Database);
    }
}
