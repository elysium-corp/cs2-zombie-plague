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

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var entries = LocalizationSeedEntries.Create();

        foreach (var key in RequiredKeys)
        {
            var seedKey = Localization.Api.LocalizationKey.Canonicalize(key);

            if (!entries.TryGetValue(seedKey, out var translations))
            {
                throw new InvalidOperationException(
                    $"Встроенный ключ локализации '{seedKey}' отсутствует."
                );
            }

            if (!translations.TryGetValue("ru", out var russianText) ||
                !translations.TryGetValue("en", out var englishText))
            {
                throw new InvalidOperationException(
                    $"Встроенный ключ локализации '{key}' должен содержать переводы ru и en."
                );
            }

            var escapedKey = Escape(seedKey);
            var module = Escape(seedKey.Split('.', 2)[0]);
            var escapedRussianText = Escape(russianText);
            var escapedEnglishText = Escape(englishText);

            migrationBuilder.Sql(
                $"""
                WITH inserted_entry AS (
                    INSERT INTO localization.entries (key, description, is_critical)
                    SELECT
                        '{escapedKey}',
                        'Системный ключ модуля {module}',
                        FALSE
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM localization.entries AS existing
                        WHERE LOWER(existing.key) = LOWER('{escapedKey}')
                    )
                    ON CONFLICT (key) DO NOTHING
                    RETURNING id
                ),
                translation_seed (language_code, text) AS (
                    VALUES
                        ('ru', '{escapedRussianText}'),
                        ('en', '{escapedEnglishText}')
                )
                INSERT INTO localization.translations (entry_id, language_code, text)
                SELECT inserted.id, language.code, seed.text
                FROM inserted_entry AS inserted
                CROSS JOIN translation_seed AS seed
                JOIN localization.languages AS language
                  ON language.code = seed.language_code
                ON CONFLICT (entry_id, language_code) DO NOTHING;
                """
            );
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
