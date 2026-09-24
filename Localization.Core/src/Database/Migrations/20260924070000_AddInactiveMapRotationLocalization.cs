using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260924070000_AddInactiveMapRotationLocalization")]
internal sealed class AddInactiveMapRotationLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TEMP TABLE map_rotation_inactive_seed(key text, ru text, en text, de text, pl text) ON COMMIT DROP;
        INSERT INTO map_rotation_inactive_seed VALUES
            ('MapRotation.InactiveNoMaps',
             'Ротация отключена: нет доступных карт в пуле. Текущая карта без ограничения времени.',
             'Rotation is inactive: no maps are available in the pool. The current map has no time limit.',
             'Die Rotation ist inaktiv: Im Kartenpool sind keine Karten verfügbar. Die aktuelle Karte hat kein Zeitlimit.',
             'Rotacja jest nieaktywna: brak dostępnych map w puli. Bieżąca mapa nie ma limitu czasu.'),
            ('MapRotation.UnlimitedTime', 'Без ограничения времени', 'No time limit', 'Kein Zeitlimit', 'Bez limitu czasu');
        INSERT INTO localization.entries(key, description, parameters, is_critical)
        SELECT key, 'Ротация карт: пустой пул и отсутствие ограничения времени', '[]'::jsonb, FALSE
        FROM map_rotation_inactive_seed
        ON CONFLICT (key) DO NOTHING;
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT entry.id, language.code, translated.text FROM map_rotation_inactive_seed seed
        JOIN localization.entries entry ON entry.key = seed.key
        CROSS JOIN LATERAL (VALUES ('ru', seed.ru), ('en', seed.en), ('de', seed.de), ('pl', seed.pl)) translated(code, text)
        JOIN localization.languages language ON language.code = translated.code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        DROP TABLE map_rotation_inactive_seed;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Пользовательские переводы сохраняются при откате.
    }
}
