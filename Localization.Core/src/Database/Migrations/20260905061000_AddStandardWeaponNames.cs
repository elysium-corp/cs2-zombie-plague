using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260905061000_AddStandardWeaponNames")]
internal sealed class AddStandardWeaponNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TEMP TABLE shop_weapon_localization_seed (
                key VARCHAR(191) PRIMARY KEY,
                name TEXT NOT NULL
            ) ON COMMIT DROP;

            INSERT INTO shop_weapon_localization_seed (key, name) VALUES
                ('Shop.Weapon.Glock.Name', 'Glock-18'),
                ('Shop.Weapon.P2000.Name', 'P2000'),
                ('Shop.Weapon.UspS.Name', 'USP-S'),
                ('Shop.Weapon.DualBerettas.Name', 'Dual Berettas'),
                ('Shop.Weapon.P250.Name', 'P250'),
                ('Shop.Weapon.Tec9.Name', 'Tec-9'),
                ('Shop.Weapon.FiveSeven.Name', 'Five-SeveN'),
                ('Shop.Weapon.Cz75Auto.Name', 'CZ75-Auto'),
                ('Shop.Weapon.DesertEagle.Name', 'Desert Eagle'),
                ('Shop.Weapon.R8Revolver.Name', 'R8 Revolver'),
                ('Shop.Weapon.Mac10.Name', 'MAC-10'),
                ('Shop.Weapon.Mp9.Name', 'MP9'),
                ('Shop.Weapon.Mp7.Name', 'MP7'),
                ('Shop.Weapon.Mp5Sd.Name', 'MP5-SD'),
                ('Shop.Weapon.Ump45.Name', 'UMP-45'),
                ('Shop.Weapon.P90.Name', 'P90'),
                ('Shop.Weapon.PpBizon.Name', 'PP-Bizon'),
                ('Shop.Weapon.GalilAr.Name', 'Galil AR'),
                ('Shop.Weapon.Famas.Name', 'FAMAS'),
                ('Shop.Weapon.Ak47.Name', 'AK-47'),
                ('Shop.Weapon.M4A4.Name', 'M4A4'),
                ('Shop.Weapon.M4A1S.Name', 'M4A1-S'),
                ('Shop.Weapon.Aug.Name', 'AUG'),
                ('Shop.Weapon.Sg553.Name', 'SG 553'),
                ('Shop.Weapon.Ssg08.Name', 'SSG 08'),
                ('Shop.Weapon.Awp.Name', 'AWP'),
                ('Shop.Weapon.Scar20.Name', 'SCAR-20'),
                ('Shop.Weapon.G3Sg1.Name', 'G3SG1'),
                ('Shop.Weapon.Nova.Name', 'Nova'),
                ('Shop.Weapon.Xm1014.Name', 'XM1014'),
                ('Shop.Weapon.Mag7.Name', 'MAG-7'),
                ('Shop.Weapon.SawedOff.Name', 'Sawed-Off'),
                ('Shop.Weapon.M249.Name', 'M249'),
                ('Shop.Weapon.Negev.Name', 'Negev');

            INSERT INTO localization.entries (key, description, is_critical, parameters)
            SELECT seed.key, 'Название обычного оружия CS2 в магазине', FALSE, '[]'::jsonb
            FROM shop_weapon_localization_seed AS seed
            WHERE NOT EXISTS (
                SELECT 1 FROM localization.entries AS existing
                WHERE lower(existing.key) = lower(seed.key));

            INSERT INTO localization.translations (entry_id, language_code, text)
            SELECT entry.id, language.code, seed.name
            FROM shop_weapon_localization_seed AS seed
            JOIN localization.entries AS entry ON lower(entry.key) = lower(seed.key)
            JOIN localization.languages AS language ON language.code IN ('ru', 'en')
            ON CONFLICT (entry_id, language_code) DO NOTHING;

            UPDATE localization.settings
            SET configuration_version = configuration_version + 1, updated_at = NOW()
            WHERE id = 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Пользовательские переводы и ссылки товаров сохраняются при откате.
    }
}
