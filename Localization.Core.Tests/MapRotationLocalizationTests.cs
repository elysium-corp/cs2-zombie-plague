using System.Text.Json;
using Localization.Api;
using Localization.Core.Application;
using Localization.Core.Configuration;
using Localization.Core.Data;
using Localization.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;

namespace Localization.Core.Tests;

public sealed class MapRotationLocalizationTests
{
    private const string InitialMigration = "20260923010000_AddMapRotationLocalization";
    private const string CompleteMigration = "20260924140000_AddPassiveMapRotationLocalization";
    private static readonly string[] RequiredSuffixes =
    [
        "Loading", "TimeLeft", "NextMap", "LastRound", "NotSelected", "RtvDelay", "RtvRemaining",
        "Accepted", "Duplicate", "Disabled", "TooFewPlayers", "NotEligible", "Locked", "InvalidMap",
        "NominationTitle", "NominationSubtitle", "VoteTitle", "VoteSubtitle", "YourVote", "Votes",
        "Close", "NoMaps", "ResultTitle", "LastRoundDescription", "ScheduledResult", "RtvAdded",
        "VoteStarted", "ForcedChange", "HudUnavailable", "RtvTitle", "RtvProgress", "CardSummary",
        "Admin.ReloadQueued", "Admin.VoteStarted", "Admin.VoteUnavailable", "Admin.NextMapSet", "Admin.InvalidMap",
        "InactiveNoMaps", "UnlimitedTime", "Inactive"
    ];

    [Theory]
    [InlineData("ru")]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("pl")]
    public void EveryFeatureKeyCanBeFormattedFromTheValidatedCentralFallback(string language)
    {
        var config = ReadConfig();
        var (api, _) = CreateApi(config);
        foreach (var suffix in RequiredSuffixes)
        {
            var key = "MapRotation." + suffix;
            Assert.Equal(key, LocalizationKey.Canonicalize(key));
            Assert.False(string.IsNullOrWhiteSpace(config.Entries[key][language]));
            var values = api.GetParameterDefinitions(key).ToDictionary(parameter => parameter.Name, parameter => (object?)parameter.Example);
            var text = api.FormatForLanguage(language, key, values);
            Assert.False(string.IsNullOrWhiteSpace(text), key);
            foreach (var parameter in values.Keys) Assert.DoesNotContain("{" + parameter + "}", text);
        }
    }

    [Fact]
    public void MissingTranslationUsesServerFallbackAndCentralParameterSanitization()
    {
        var config = ReadConfig();
        config.ServerFallbackLanguage = "en";
        config.Entries["MapRotation.RtvAdded"].Remove("ru");
        config.Checksum = FallbackConfigChecksum.Compute(config);
        var (api, _) = CreateApi(config);

        var text = api.FormatForLanguage("ru", "MapRotation.RtvAdded", new Dictionary<string, object?>
        {
            ["player"] = "[red]Player[/]", ["votes"] = "4", ["required"] = "7"
        });

        Assert.Equal("Player requested RTV (4 / 7)", text);
        Assert.Null(api.FormatForLanguage("en", "MapRotation.Admin.NextMapSet", new Dictionary<string, object?>()));
    }

    [Fact]
    public void ChangedCatalogTextIsUsedAfterSnapshotReload()
    {
        var config = ReadConfig();
        var (api, cache) = CreateApi(config);
        var values = new Dictionary<string, object?> { ["count"] = "8" };
        Assert.Equal("Голосов: 8", api.FormatForLanguage("ru", "MapRotation.Votes", values));

        config.Entries["MapRotation.Votes"]["ru"] = "Выбрали карту: {count}";
        config.Checksum = FallbackConfigChecksum.Compute(config);
        cache.Replace(FallbackLocalizationProvider.Load(config));

        Assert.Equal("Выбрали карту: 8", api.FormatForLanguage("ru", "MapRotation.Votes", values));
    }

    [Fact]
    public void MigrationsSeedAllKeysAndPreserveExistingTranslations()
    {
        var sql = GenerateScript("20260920180000_AddElysiumKnifeHudLocalization", CompleteMigration);
        foreach (var suffix in RequiredSuffixes) Assert.Contains("'MapRotation." + suffix + "'", sql);

        var upgrade = GenerateScript(InitialMigration, CompleteMigration);
        Assert.Contains("ON CONFLICT (key) DO NOTHING", upgrade);
        Assert.Contains("ON CONFLICT (entry_id, language_code) DO NOTHING", upgrade);
        Assert.Contains("configuration_version = configuration_version + 1", upgrade);
        Assert.DoesNotContain("UPDATE localization.translations", upgrade, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM localization", upgrade, StringComparison.OrdinalIgnoreCase);

        var downgrade = GenerateScript(CompleteMigration, InitialMigration);
        Assert.DoesNotContain("DELETE FROM localization", downgrade, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE localization.translations", downgrade, StringComparison.OrdinalIgnoreCase);
    }

    private static (ILocalizationApi Api, LocalizationCache Cache) CreateApi(LocalizationFallbackConfig config)
    {
        var cache = new LocalizationCache();
        cache.Replace(FallbackLocalizationProvider.Load(config));
        var resolver = new LanguageResolver(cache, new PlayerLanguageCache());
        var runtime = new LocalizationRuntime(cache, resolver, new RateLimitedLocalizationLogger(NullLogger.Instance));
        return (new Localization.Core.Api.LocalizationApi(cache, resolver, runtime), cache);
    }

    private static LocalizationFallbackConfig ReadConfig() => JsonSerializer.Deserialize<LocalizationFallbackConfig>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "template.jsonc")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    private static string GenerateScript(string from, string to)
    {
        using var context = new LocalizationDbContext(new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseNpgsql("Host=localhost;Database=map_rotation_localization_test;Username=test;Password=test").Options);
        return context.GetService<IMigrator>().GenerateScript(from, to);
    }
}
