using System.Text.Json;
using Localization.Core.Configuration;
using Localization.Core.Data;
using Localization.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Tests;

public sealed class EquipmentLocalizationContractTests
{
    private const string PreviousMigration = "20260831050000_AddLocalizationColorTags";
    private const string FeatureMigration = "20260901223000_AddEquipmentLocalizationEntries";
    private const string TagCleanupMigration = "20260904090000_RemoveAdvertisementTagLocalizationEntries";
    private const string TagOwnershipMigration = "20260904120000_OwnAdvertisementTags";

    private static readonly string[] RequiredKeys =
    [
        "Menu.Main.Item.Knife.Title",
        "Menu.Knife.Title",
        "Menu.Knife.Selected",
        "Menu.Knife.SelectionSuccess",
        "Menu.Knife.PermissionRequired",
        "CustomKnife.knife_piercer.Name",
        "CustomKnife.knife_piercer.Description",
        "CustomKnife.knife_spike.Name",
        "CustomKnife.knife_spike.Description",
        "CustomKnife.knife_axe.Name",
        "CustomKnife.knife_axe.Description",
        "CustomKnife.knife_katana.Name",
        "CustomKnife.knife_katana.Description",
        "Menu.Main.Item.Equipment.Title",
        "Menu.Equipment.Title",
        "Menu.Equipment.Back",
        "Menu.Equipment.Category.Pistol",
        "Menu.Equipment.Category.SubmachineGun",
        "Menu.Equipment.Category.Rifle",
        "Menu.Equipment.Category.Shotgun",
        "Menu.Equipment.Category.SniperRifle",
        "Menu.Equipment.Category.MachineGun",
        "Menu.Equipment.Category.Grenade",
        "Menu.Equipment.Category.Equipment",
        "Equipment.Shop.Human.Title",
        "Equipment.Shop.Zombie.Title",
        "Equipment.Errors.RoleUnavailable",
        "Equipment.Errors.NotEnoughMoney",
        "Equipment.Errors.ShopRoundLimit",
        "Equipment.Errors.ShopMapLimit",
        "Equipment.Errors.ItemRoundLimit",
        "Equipment.Errors.ItemMapLimit",
        "Equipment.Errors.ShopUnavailable",
        "Equipment.LaserMine.AlreadyOwned",
        "Equipment.LaserMine.Granted",
        "Equipment.LaserMine.Installing",
        "Equipment.LaserMine.InvalidSurface",
        "Equipment.Item.custom_equipment.armor.Name",
        "Equipment.Item.custom_equipment.laser_mine.Name",
        "Equipment.Item.custom_equipment.shake_nade.Name",
        "Equipment.Item.custom_equipment.jump_nade.Name",
        "Equipment.Item.custom_equipment.barrier_nade.Name",
        "Equipment.Item.custom_equipment.frost_nade.Name",
        "Equipment.Item.custom_equipment.fire_nade.Name",
        "Equipment.Item.custom_equipment.ajm.Name",
        "Equipment.Item.custom_equipment.blackline.Name",
        "Equipment.Item.custom_equipment.elite.Name",
        "Equipment.Item.custom_equipment.frostbyte.Name",
        "Equipment.Item.custom_equipment.lava.Name",
        "Equipment.Item.custom_equipment.omega.Name",
        "Equipment.Item.custom_equipment.reactorleak.Name",
        "Equipment.Item.custom_equipment.reaver.Name",
        "Equipment.Item.custom_equipment.x3.Name",
        "Ammo.Warning.NotEnoughMoney",
        "Ammo.Warning.EnoughAmmo",
    ];

    [Fact]
    public void FallbackTemplate_ContainsRequiredEquipmentAndKnifeTranslations()
    {
        var snapshot = FallbackLocalizationProvider.Load(ReadFallbackConfig());

        foreach (var key in RequiredKeys)
        {
            Assert.Contains(key, snapshot.Entries.Keys);
            var entry = snapshot.Entries[key];
            Assert.Contains("ru", entry.Translations.Keys);
            Assert.Contains("en", entry.Translations.Keys);
        }
    }

    [Theory]
    [InlineData("ajm", "CZ75 Ajm")]
    [InlineData("blackline", "MP9 Blackline")]
    [InlineData("elite", "SSG Elite")]
    [InlineData("frostbyte", "MP7 Frostbyte")]
    [InlineData("lava", "AK47 Lava")]
    [InlineData("omega", "Omega Shotgun")]
    [InlineData("reactorleak", "UMP45 ReactorLeak")]
    [InlineData("reaver", "Deagle Reaver")]
    [InlineData("x3", "M4A1-S X3")]
    public void FallbackTemplate_UsesExpectedDisplayNamesForStockWeapons(
        string weaponId,
        string displayName
    )
    {
        var snapshot = FallbackLocalizationProvider.Load(ReadFallbackConfig());
        var entry = snapshot.Entries[$"Equipment.Item.custom_equipment.{weaponId}.Name"];

        Assert.Equal(displayName, entry.Translations["ru"]);
        Assert.Equal(displayName, entry.Translations["en"]);
    }

    [Fact]
    public void UpgradeScript_InsertsOnlyAbsentLogicalKeysWithoutChangingAdminEntries()
    {
        var script = GenerateScript(PreviousMigration, FeatureMigration);

        foreach (var key in RequiredKeys)
        {
            Assert.Contains($"'{key}'", script);
            Assert.Contains(
                $"WHERE LOWER(existing.key) = LOWER('{key}')",
                script
            );
        }

        Assert.Equal(
            RequiredKeys.Length,
            CountOccurrences(script, "WITH inserted_entry AS")
        );
        Assert.Equal(
            RequiredKeys.Length,
            CountOccurrences(script, "FROM inserted_entry AS inserted")
        );
        Assert.Contains("ON CONFLICT (key) DO NOTHING", script);
        Assert.Contains("ON CONFLICT (entry_id, language_code) DO NOTHING", script);
        Assert.DoesNotContain("DO UPDATE SET", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("configuration_version = configuration_version + 1", script);
    }

    [Fact]
    public void DowngradeScript_DoesNotDeleteAdministratorTranslations()
    {
        var script = GenerateScript(FeatureMigration, PreviousMigration);

        Assert.DoesNotContain(
            "DELETE FROM localization.entries",
            script,
            StringComparison.OrdinalIgnoreCase
        );
        Assert.DoesNotContain(
            "DELETE FROM localization.translations",
            script,
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public void TagCleanupMigration_MovesTranslationsBeforeRemovingDuplicateKeys()
    {
        var script = GenerateScript(FeatureMigration, TagCleanupMigration);

        Assert.Contains("INSERT INTO advertisement.tag_translations", script);
        Assert.Contains("ON CONFLICT (tag_id, locale) DO UPDATE", script);
        Assert.Contains("DELETE FROM localization.entries", script);
        Assert.Contains("advertisement.tags.%", script);
    }

    [Fact]
    public void TagOwnershipMigration_MovesDefinitionsToLocalizationAndLeavesReferencesInAdvertisement()
    {
        var script = GenerateScript(TagCleanupMigration, TagOwnershipMigration);

        Assert.Contains("CREATE TABLE IF NOT EXISTS localization.tags", script);
        Assert.Contains("'Tags.' || lower(tag.key)", script);
        Assert.Contains("ADD COLUMN IF NOT EXISTS tag_key", script);
        Assert.Contains("FOREIGN KEY (tag_key)", script);
        Assert.Contains("REFERENCES localization.tags(key)", script);
        Assert.Contains("DROP TABLE IF EXISTS advertisement.tag_translations", script);
        Assert.Contains("DROP TABLE IF EXISTS advertisement.tags", script);
    }

    private static string GenerateScript(string fromMigration, string toMigration)
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=localization_migration_test;Username=test;Password=test"
            )
            .Options;
        using var context = new LocalizationDbContext(options);
        return context.GetService<IMigrator>().GenerateScript(fromMigration, toMigration);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var offset = 0;

        while ((offset = text.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
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
