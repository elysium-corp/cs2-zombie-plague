using System.Text.Json;
using Localization.Api;
using Localization.Core.Application;
using Localization.Core.Configuration;
using Localization.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Tests;

public sealed class AbilityHudLocalizationTests
{
    [Fact]
    public void HudSettingsHaveCanonicalRussianAndEnglishFallbackKeys()
    {
        var config = JsonSerializer.Deserialize<LocalizationFallbackConfig>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "template.jsonc")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        LocalizationValidation.ValidateFallback(config);
        var keys = config.Entries.Where(item => item.Key.StartsWith("Menu.AbilityHud.")).ToArray();
        Assert.Equal(17, keys.Length);
        Assert.Equal(9, keys.Count(item => item.Key.StartsWith("Menu.AbilityHud.Position.")));
        foreach (var (key, translations) in keys)
        {
            Assert.Equal(key, LocalizationKey.Canonicalize(key));
            Assert.False(string.IsNullOrWhiteSpace(translations["ru"]));
            Assert.False(string.IsNullOrWhiteSpace(translations["en"]));
        }
    }

    [Fact]
    public void MigrationSeedsSettingsWithoutReplacingExistingTranslations()
    {
        using var context = new LocalizationDbContext(new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseNpgsql("Host=localhost;Database=hud_localization_test;Username=test;Password=test").Options);
        var sql = context.GetService<IMigrator>().GenerateScript("20260906120000_AddHumanClassAndAbilityLocalization", "20260906160000_AddAbilityHudSettingsLocalization");
        Assert.Contains("Menu.AbilityHud.Position.Top.Left", sql);
        Assert.Contains("ON CONFLICT (entry_id, language_code) DO NOTHING", sql);
        Assert.DoesNotContain("UPDATE localization.translations", sql);
        Assert.DoesNotContain("DELETE FROM localization", sql);
    }
}
