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
        "Ammo.Warning.NotEnoughMoney",
        "Ammo.Warning.EnoughAmmo",
    ];

    [Fact]
    public void EmergencySnapshot_ContainsRequiredEquipmentAndKnifeTranslations()
    {
        var snapshot = FallbackLocalizationProvider.Load(new LocalizationFallbackConfig());

        foreach (var key in RequiredKeys)
        {
            Assert.Contains(key, snapshot.Entries.Keys);
            var entry = snapshot.Entries[key];
            Assert.Contains("ru", entry.Translations.Keys);
            Assert.Contains("en", entry.Translations.Keys);
        }
    }

    [Fact]
    public void UpgradeScript_AddsMissingTranslationsWithoutOverwritingAdminText()
    {
        var script = GenerateScript(PreviousMigration, FeatureMigration);

        foreach (var key in RequiredKeys)
        {
            Assert.Contains($"'{key}'", script);
        }

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
}
