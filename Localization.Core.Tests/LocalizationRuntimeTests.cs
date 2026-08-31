using Localization.Core.Application;
using Localization.Core.Data;
using Localization.Api;
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

    [Fact]
    public void TypedParameters_AreValidatedAndFormattedInvariantly()
    {
        var cache = new LocalizationCache();
        cache.Replace(CreateSnapshot());
        var runtime = new LocalizationRuntime(
            cache,
            new LanguageResolver(cache, new PlayerLanguageCache()),
            new RateLimitedLocalizationLogger(NullLogger.Instance));

        var valid = runtime.FormatForLanguage(
            "en",
            "test.reward",
            new Dictionary<string, object?> { ["points"] = 15 });
        var invalid = runtime.FormatForLanguage(
            "en",
            "test.reward",
            new Dictionary<string, object?> { ["points"] = "fifteen" });
        var missing = runtime.FormatForLanguage(
            "en",
            "test.reward",
            new Dictionary<string, object?>());

        Assert.Equal("{success}+15{/success}", valid);
        Assert.Null(invalid);
        Assert.Null(missing);
        Assert.Equal(LocalizationParameterType.Integer, runtime.GetParameterDefinitions("test.reward")[0].Type);
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
            CreateEntry(
                1,
                "economy.errors.insufficient_money",
                new Dictionary<string, string>
                {
                    ["ru"] = "Недостаточно средств",
                    ["en"] = "Not enough money",
                }),
            CreateEntry(
                2,
                "test.reward",
                new Dictionary<string, string>
                {
                    ["ru"] = "{success}+{points}{/success}",
                    ["en"] = "{success}+{points}{/success}",
                },
                [new LocalizationParameterDefinition(
                    "points",
                    LocalizationParameterType.Integer,
                    true,
                    "Количество очков",
                    "15")]),
        }.ToFrozenDictionary(entry => entry.Key, StringComparer.OrdinalIgnoreCase);

        return new LocalizationSnapshot(
            new LocalizationSettings("ru", 30, true, true, 1),
            languages,
            entries,
            DateTimeOffset.UtcNow,
            LocalizationSource.Database);
    }

    private static LocalizationEntry CreateEntry(
        long id,
        string key,
        Dictionary<string, string> values,
        IReadOnlyList<LocalizationParameterDefinition>? parameters = null)
    {
        var translations = values.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        return new LocalizationEntry(
            id,
            key,
            false,
            translations,
            LocalizationParameterSchema.Normalize(parameters, translations));
    }
}
