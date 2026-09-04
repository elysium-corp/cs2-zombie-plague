using System.Text.Json;
using Localization.Api;
using Localization.Core.Configuration;
using Localization.Core.Data;
using Localization.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Tests;

public sealed class ShopLocalizationContractTests
{
    private const string PreviousMigration = "20260904172000_EnsureFallbackExportMarker";
    private const string FeatureMigration = "20260904183500_AddShopLocalizationEntries";

    private static readonly string[] RequiredKeys =
    [
        "Shop.Human.Title",
        "Shop.Zombie.Title",
        "Shop.Menu.Balance",
        "Shop.Menu.Price",
        "Shop.Menu.Ammo",
        "Shop.Menu.Empty",
        "Shop.Menu.Back",
        "Shop.Commands.Reload.Help",
        "Shop.Commands.Status.Help",
        "Shop.Admin.Reload.Started",
        "Shop.Admin.Reload.Succeeded",
        "Shop.Admin.Reload.Failed",
        "Shop.Admin.Status",
        "Shop.Item.Unknown.Name",
        "Shop.Errors.Unavailable",
        "Shop.Errors.ProductUnavailable",
        "Shop.Errors.TeamUnavailable",
        "Shop.Errors.AccessDenied",
        "Shop.Errors.NotEnoughMoney",
        "Shop.Errors.RoundLimit",
        "Shop.Errors.MapLimit",
        "Shop.Errors.Cooldown",
        "Shop.Errors.InvalidPlayer",
        "Shop.Errors.Cancelled",
        "Shop.Errors.PaymentRejected",
        "Shop.Errors.GrantRejected",
        "Shop.Errors.RefundFailed",
        "Shop.Errors.AmmoNotConfigured",
        "Shop.Errors.AmmoFull"
    ];

    [Fact]
    public void FallbackTemplateContainsEveryShopRuntimeKey()
    {
        var snapshot = FallbackLocalizationProvider.Load(ReadFallbackConfig());

        foreach (var key in RequiredKeys)
        {
            Assert.True(snapshot.Entries.ContainsKey(key), $"Missing localization key '{key}'.");
            var entry = snapshot.Entries[key];
            Assert.True(entry.Translations.ContainsKey("ru"));
            Assert.True(entry.Translations.ContainsKey("en"));
        }

        Assert.Equal(
            LocalizationParameterType.Integer,
            snapshot.Entries["Shop.Menu.Balance"].Parameters["balance"].Type);
        Assert.Equal(
            ["amount", "price"],
            snapshot.Entries["Shop.Menu.Ammo"].Parameters.Keys
                .Order(StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            ["categories", "loaded", "offers", "source"],
            snapshot.Entries["Shop.Admin.Status"].Parameters.Keys
                .Order(StringComparer.Ordinal)
                .ToArray());
    }

    [Fact]
    public void MigrationSeedsShopKeysWithoutOverwritingTranslations()
    {
        var script = GenerateScript(PreviousMigration, FeatureMigration);

        foreach (var key in RequiredKeys)
        {
            Assert.Contains($"'{key}'", script);
        }

        Assert.Contains("ON CONFLICT (entry_id, language_code) DO NOTHING", script);
        Assert.DoesNotContain(
            "UPDATE localization.translations",
            script,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("configuration_version = configuration_version + 1", script);
    }

    [Fact]
    public void DowngradeDoesNotDeleteAdministratorTranslations()
    {
        var script = GenerateScript(FeatureMigration, PreviousMigration);

        Assert.DoesNotContain(
            "DELETE FROM localization.entries",
            script,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "DELETE FROM localization.translations",
            script,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GenerateScript(string fromMigration, string toMigration)
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=localization_migration_test;Username=test;Password=test")
            .Options;
        using var context = new LocalizationDbContext(options);
        return context.GetService<IMigrator>().GenerateScript(fromMigration, toMigration);
    }

    private static LocalizationFallbackConfig ReadFallbackConfig()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "template.jsonc");
        return JsonSerializer.Deserialize<LocalizationFallbackConfig>(
                   File.ReadAllText(path),
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidDataException("Не удалось прочитать fixture localization.json.");
    }
}
