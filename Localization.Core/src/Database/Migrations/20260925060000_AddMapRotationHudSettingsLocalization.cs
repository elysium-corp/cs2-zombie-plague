using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260925060000_AddMapRotationHudSettingsLocalization")]
internal sealed class AddMapRotationHudSettingsLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TEMP TABLE map_rotation_hud_settings_seed(key text, ru text, en text, de text, pl text) ON COMMIT DROP;
        INSERT INTO map_rotation_hud_settings_seed VALUES
            ('MapRotation.Settings.Title', 'Настройки меню', 'Menu settings', 'Menüeinstellungen', 'Ustawienia menu'),
            ('MapRotation.Settings.Orientation', 'Ориентация', 'Orientation', 'Ausrichtung', 'Orientacja'),
            ('MapRotation.Settings.Horizontal', 'Горизонтально', 'Horizontal', 'Horizontal', 'Poziomo'),
            ('MapRotation.Settings.Vertical', 'Вертикально', 'Vertical', 'Vertikal', 'Pionowo'),
            ('MapRotation.Settings.Size', 'Размер меню', 'Menu size', 'Menügröße', 'Rozmiar menu'),
            ('MapRotation.Settings.Scale80', '80%', '80%', '80%', '80%'),
            ('MapRotation.Settings.Scale100', '100%', '100%', '100%', '100%'),
            ('MapRotation.Settings.Scale120', '120%', '120%', '120%', '120%'),
            ('MapRotation.YourNomination', 'Ваша номинация', 'Your nomination', 'Deine Nominierung', 'Twoja nominacja');
        INSERT INTO localization.entries(key, description, parameters, is_critical)
        SELECT key, 'Ротация карт: настройки меню и выбранная номинация', '[]'::jsonb, FALSE FROM map_rotation_hud_settings_seed
        ON CONFLICT (key) DO NOTHING;
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT entry.id, language.code, translated.text FROM map_rotation_hud_settings_seed seed
        JOIN localization.entries entry ON entry.key = seed.key
        CROSS JOIN LATERAL (VALUES ('ru', seed.ru), ('en', seed.en), ('de', seed.de), ('pl', seed.pl)) translated(code, text)
        JOIN localization.languages language ON language.code = translated.code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        DROP TABLE map_rotation_hud_settings_seed;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Пользовательские переводы сохраняются при откате.
    }
}
