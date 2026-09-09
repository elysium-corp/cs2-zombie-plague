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

    [Fact]
    public void ExplicitHtmlEscapesPlayerTextOnceAndKeepsNumbersTyped()
    {
        var cache = new LocalizationCache(); cache.Replace(CreateSnapshot());
        var runtime = new LocalizationRuntime(cache, new LanguageResolver(cache, new PlayerLanguageCache()), new RateLimitedLocalizationLogger(NullLogger.Instance));
        var result = runtime.FormatForLanguage("en", "Test.Player", new Dictionary<string, object?> { ["nickname"] = "<b>A&B [red]{success}" }, LocalizationOutputMode.Html);
        Assert.Equal("Player: <span class=\"hud-parameter\">&lt;b&gt;A&amp;B </span>", result);
        var reward = runtime.FormatForLanguage("en", "Test.Reward", new Dictionary<string, object?> { ["points"] = 15 }, LocalizationOutputMode.Html);
        Assert.Contains("<span class=\"hud-parameter\">15</span>", reward);
        Assert.Null(runtime.FormatForLanguage("en", "Test.Reward", new Dictionary<string, object?> { ["points"] = "bad" }, LocalizationOutputMode.Html));
    }

    [Fact]
    public void CountdownParameterKeepsSurroundingHtmlColorAndBold()
    {
        var cache = new LocalizationCache(); cache.Replace(CreateSnapshot());
        var runtime = new LocalizationRuntime(cache, new LanguageResolver(cache, new PlayerLanguageCache()), new RateLimitedLocalizationLogger(NullLogger.Instance));
        var result = runtime.FormatForLanguage("ru", "Test.Countdown", new Dictionary<string, object?> { ["seconds"] = 10 }, LocalizationOutputMode.Html);
        Assert.Equal("До заражения <font color=\"red\"><b><span class=\"hud-parameter\">10</span></b></font> сек.", result);
    }

    [Fact]
    public void RoleMarkupUsesRecipientStyleForHtmlAndChatAndSupportsRawMode()
    {
        const string input = "{role_color}<b>Player</b>{/role_color}<br><font color='role'>Role</font>";
        var style = new LocalizationPlayerStyle("admin.owner", "Owner", "red", "#ff4040");
        var html = LocalizationMarkupRenderer.Render(input, LocalizationColorSchema.Defaults, LocalizationOutputMode.Html, style);
        Assert.Contains("<font color=\"#ff4040\"><b>Player</b></font>", html);
        Assert.DoesNotContain("role_color", html);
        var chat = LocalizationMarkupRenderer.Render(input, LocalizationColorSchema.Defaults, LocalizationOutputMode.Chat, style);
        Assert.Contains("[red]Player", chat); Assert.DoesNotContain("<", chat);
        Assert.Equal(input, LocalizationMarkupRenderer.Render(input, LocalizationColorSchema.Defaults, LocalizationOutputMode.Raw, style));
        Assert.Contains("#ffffff", LocalizationMarkupRenderer.Render(input, LocalizationColorSchema.Defaults, LocalizationOutputMode.Html));
        Assert.False(LocalizationHtmlMarkup.IsValid("<span onclick='x'>Bad</span>"));
        Assert.False(LocalizationHtmlMarkup.IsValid("<b><i>Bad</b></i>"));
    }

    [Fact]
    public void RoleAppearanceSelectsHighestPriorityWithStableTieBreakAndSafeFallback()
    {
        Assert.Equal(new LocalizationPlayerStyle(), LocalizationRoleStyle.Select([]));
        var roles = new Admin.Api.Data.IPrivilege[] { new TestRole("vip.premium", 10), new TestRole("admin.z", 100), new TestRole("admin.a", 100) };
        Assert.Equal("admin.a", LocalizationRoleStyle.Select(roles).RoleKey);
        Assert.Equal("#ffffff", LocalizationRoleStyle.Select([new TestRole("broken", 500, "bad")]).HudColor);
    }

    [Fact]
    public void LegacyChatColorsAndHtmlNestedStylesSurviveHudFormatting()
    {
        var result = LocalizationMarkupRenderer.Render("[red]<b>Red <i>italic</i></b>[/] plain", LocalizationColorSchema.Defaults, LocalizationOutputMode.Html);
        Assert.Contains("<font color=\"red\"><b>Red </b></font>", result);
        Assert.Contains("<font color=\"red\"><b><i>italic</i></b></font>", result);
        Assert.EndsWith(" plain", result); Assert.DoesNotContain("[red]", result);
    }

    private sealed record TestRole(string Key, int ColorPriority, string HudColor = "#ff4040") : Admin.Api.Data.IPrivilege
    {
        public string Id => Key; public string Group => "test";
        public string DisplayName => Key; public string ChatColor => "red";
        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();
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
            CreateEntry(
                6,
                "Test.Countdown",
                new Dictionary<string, string>
                {
                    ["ru"] = "До заражения <font color=\"red\"><b>{seconds}</b></font> сек.",
                },
                [new LocalizationParameterDefinition("seconds", LocalizationParameterType.Integer, true, "До заражения", "10")]),
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
