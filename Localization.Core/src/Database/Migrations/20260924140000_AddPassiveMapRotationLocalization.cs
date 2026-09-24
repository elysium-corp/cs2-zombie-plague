using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260924140000_AddPassiveMapRotationLocalization")]
internal sealed class AddPassiveMapRotationLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        INSERT INTO localization.entries(key, description, parameters, is_critical)
        VALUES ('MapRotation.Inactive', 'Ротация карт: пассивный режим без доступного пула', '[]'::jsonb, FALSE)
        ON CONFLICT (key) DO NOTHING;
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT entry.id, language.code, translated.text
        FROM localization.entries entry
        CROSS JOIN (VALUES
            ('ru', 'Ротация отключена: нет доступных карт в пуле.'),
            ('en', 'Rotation is inactive: no maps are available in the pool.'),
            ('de', 'Die Rotation ist inaktiv: Im Kartenpool sind keine Karten verfügbar.'),
            ('pl', 'Rotacja jest nieaktywna: brak dostępnych map w puli.')
        ) translated(code, text)
        JOIN localization.languages language ON language.code = translated.code
        WHERE entry.key = 'MapRotation.Inactive'
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Пользовательские переводы сохраняются при откате.
    }
}
