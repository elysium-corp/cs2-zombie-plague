using Localization.Core.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260901223000_AddEquipmentLocalizationEntries")]
internal sealed class AddEquipmentLocalizationEntries : Migration
{
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
        "Equipment.Item.custom_equipment.laser_mine.Name",
        "Equipment.Item.custom_equipment.shake_nade.Name",
        "Equipment.Item.custom_equipment.jump_nade.Name",
        "Equipment.Item.custom_equipment.barrier_nade.Name",
        "Equipment.Item.custom_equipment.frost_nade.Name",
        "Equipment.Item.custom_equipment.fire_nade.Name",
        "Equipment.Item.custom_equipment.armor.Name",
        "Ammo.Warning.NotEnoughMoney",
        "Ammo.Warning.EnoughAmmo",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var entries = BuiltInLocalizationEntries.Create();

        foreach (var key in RequiredKeys)
        {
            if (!entries.TryGetValue(key, out var translations))
            {
                throw new InvalidOperationException(
                    $"Встроенный ключ локализации '{key}' отсутствует."
                );
            }

            var escapedKey = Escape(key);
            var module = Escape(key.Split('.', 2)[0]);

            migrationBuilder.Sql(
                $"""
                INSERT INTO localization.entries (key, description, is_critical)
                VALUES (
                    '{escapedKey}',
                    'Системный ключ модуля {module}',
                    FALSE
                )
                ON CONFLICT (key) DO NOTHING;
                """
            );

            foreach (var (language, text) in translations)
            {
                migrationBuilder.Sql(
                    $"""
                    INSERT INTO localization.translations (entry_id, language_code, text)
                    SELECT entry.id, language.code, '{Escape(text)}'
                    FROM localization.entries AS entry
                    JOIN localization.languages AS language
                      ON language.code = '{Escape(language)}'
                    WHERE entry.key = '{escapedKey}'
                    ON CONFLICT (entry_id, language_code) DO NOTHING;
                    """
                );
            }
        }

        migrationBuilder.Sql(
            """
            UPDATE localization.settings
            SET configuration_version = configuration_version + 1,
                updated_at = NOW()
            WHERE id = 1;
            """
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Администратор мог изменить эти записи после миграции; данные не удаляем.
    }

    private static string Escape(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);
}
