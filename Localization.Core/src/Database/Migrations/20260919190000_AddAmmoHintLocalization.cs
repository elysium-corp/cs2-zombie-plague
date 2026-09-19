using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260919190000_AddAmmoHintLocalization")]
internal sealed class AddAmmoHintLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        INSERT INTO localization.entries(key, description, parameters, is_critical)
        VALUES ('Notifications.Shop.Ammo.Empty', 'Подсказка покупки патронов при пустом магазине и резерве', '[]'::jsonb, FALSE)
        ON CONFLICT (key) DO NOTHING;
        WITH seed(language_code, text) AS (VALUES
            ('ru', 'Купить патроны'), ('en', 'Buy ammo'),
            ('de', 'Munition kaufen'), ('pl', 'Kup amunicję'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT entry.id, language.code, seed.text
        FROM seed JOIN localization.languages language ON language.code = seed.language_code
        CROSS JOIN localization.entries entry
        WHERE entry.key = 'Notifications.Shop.Ammo.Empty'
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Сохраняем переводы и пользовательские ссылки при откате.
    }
}
