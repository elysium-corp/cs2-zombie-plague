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

        var text = runtime.GetForLanguage("de", "Economy.Errors.Insufficient.Money", null);

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
            "Test.Reward",
            new Dictionary<string, string> { ["points"] = "15" });

        Assert.Equal("[green]+15[default][/]", text);
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
            "Test.Reward",
            new Dictionary<string, object?> { ["points"] = 15 });
        var validString = runtime.FormatForLanguage(
            "en",
            "Test.Reward",
            new Dictionary<string, object?> { ["points"] = "15" });
        var invalid = runtime.FormatForLanguage(
            "en",
            "Test.Reward",
            new Dictionary<string, object?> { ["points"] = "fifteen" });
        var missing = runtime.FormatForLanguage(
            "en",
            "Test.Reward",
            new Dictionary<string, object?>());

        Assert.Equal("[green]+15[default][/]", valid);
        Assert.Equal("[green]+15[default][/]", validString);
        Assert.Null(invalid);
        Assert.Null(missing);
        Assert.Equal(LocalizationParameterType.Integer, runtime.GetParameterDefinitions("Test.Reward")[0].Type);
        Assert.False(LocalizationParameterSchema.TryFormatValue(
            LocalizationParameterType.String,
            15,
            out _));
    }

    [Fact]
    public void CustomColorTag_IsRenderedWithConfiguredSwiftlyColor()
    {
        var cache = new LocalizationCache();
        cache.Replace(CreateSnapshot());
        var runtime = new LocalizationRuntime(
            cache,
            new LanguageResolver(cache, new PlayerLanguageCache()),
            new RateLimitedLocalizationLogger(NullLogger.Instance));

        var text = runtime.GetForLanguage("ru", "Test.Vip", null);

        Assert.Equal("[gold]VIP игрок[default][/]", text);
    }

    [Fact]
    public void ParameterValue_CannotInjectSwiftlyOrSemanticColorMarkup()
    {
        var cache = new LocalizationCache();
        cache.Replace(CreateSnapshot());
        var runtime = new LocalizationRuntime(
            cache,
            new LanguageResolver(cache, new PlayerLanguageCache()),
            new RateLimitedLocalizationLogger(NullLogger.Instance));

        var text = runtime.FormatForLanguage(
            "en",
            "Test.Player",
            new Dictionary<string, object?>
            {
                ["nickname"] = "[red]{warning}fdrinv{/warning}[/]",
            });

        Assert.Equal("Player: fdrinv", text);
    }

    [Fact]
    public void Tag_IsResolvedFromLocalizationSnapshotForRequestedLanguage()
    {
        var cache = new LocalizationCache();
        cache.Replace(CreateSnapshot());
        var runtime = new LocalizationRuntime(
            cache,
            new LanguageResolver(cache, new PlayerLanguageCache()),
            new RateLimitedLocalizationLogger(NullLogger.Instance));

        var english = runtime.GetTagForLanguage("en", "Elysium");
        var fallback = runtime.GetTagForLanguage("de", "Elysium");

        Assert.Equal(new LocalizationTag("Elysium", "Elysium", "purple"), english);
        Assert.Equal(new LocalizationTag("Elysium", "Элизиум", "purple"), fallback);
        Assert.Null(runtime.GetTagForLanguage("ru", "missing"));
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
                "Economy.Errors.Insufficient.Money",
                new Dictionary<string, string>
                {
                    ["ru"] = "Недостаточно средств",
                    ["en"] = "Not enough money",
                }),
            CreateEntry(
                2,
                "Test.Reward",
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
            CreateEntry(
                3,
                "Test.Vip",
                new Dictionary<string, string>
                {
                    ["ru"] = "{vip}VIP игрок{/vip}",
                    ["en"] = "{vip}VIP player{/vip}",
                }),
            CreateEntry(
                4,
                "Test.Player",
                new Dictionary<string, string>
                {
                    ["ru"] = "Игрок: {nickname}",
                    ["en"] = "Player: {nickname}",
                },
                [new LocalizationParameterDefinition(
                    "nickname",
                    LocalizationParameterType.String,
                    true,
                    "Ник игрока",
                    "fdrinv")]),
            CreateEntry(
                5,
                "Tag.Elysium",
                new Dictionary<string, string>
                {
                    ["ru"] = "Элизиум",
                    ["en"] = "Elysium",
                }),
        }.ToFrozenDictionary(entry => entry.Key, StringComparer.OrdinalIgnoreCase);

        var colorTags = LocalizationColorSchema.Defaults
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        colorTags["vip"] = "gold";

        return new LocalizationSnapshot(
            new LocalizationSettings(
                "ru",
                30,
                true,
                1,
                colorTags.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)),
            languages,
            entries,
            new[]
            {
                new LocalizationTagState(1, "Elysium", "Tag.Elysium", "purple", true, 0),
            }.ToFrozenDictionary(tag => tag.Key, StringComparer.OrdinalIgnoreCase),
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
