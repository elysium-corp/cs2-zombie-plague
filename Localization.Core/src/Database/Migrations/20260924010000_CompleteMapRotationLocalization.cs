using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260924010000_CompleteMapRotationLocalization")]
internal sealed class CompleteMapRotationLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TEMP TABLE map_rotation_interface_seed(key text, ru text, en text, de text, pl text, parameters jsonb) ON COMMIT DROP;
        INSERT INTO map_rotation_interface_seed VALUES
            ('MapRotation.RtvTitle', 'RTV', 'RTV', 'RTV', 'RTV', '[]'::jsonb),
            ('MapRotation.RtvProgress', '{votes} / {required}', '{votes} / {required}', '{votes} / {required}', '{votes} / {required}', '[{"name": "votes", "type": "string", "required": true, "description": "Количество полученных голосов", "example": "4"}, {"name": "required", "type": "string", "required": true, "description": "Необходимое количество голосов", "example": "7"}]'::jsonb),
            ('MapRotation.CardSummary', '{title}: {value} {description}', '{title}: {value} {description}', '{title}: {value} {description}', '{title}: {value} {description}', '[{"name": "title", "type": "string", "required": true, "description": "Локализованный заголовок карточки", "example": "RTV"}, {"name": "value", "type": "string", "required": true, "description": "Основное значение карточки", "example": "4 / 7"}, {"name": "description", "type": "string", "required": true, "description": "Локализованная дополнительная строка карточки", "example": "Осталось голосов: 3"}]'::jsonb),
            ('MapRotation.Admin.ReloadQueued', 'Обновление настроек и каталога карт поставлено в очередь', 'Map settings and catalog reload queued', 'Neuladen der Karteneinstellungen und des Kartenkatalogs vorgemerkt', 'Dodano do kolejki odświeżenie ustawień i katalogu map', '[]'::jsonb),
            ('MapRotation.Admin.VoteStarted', 'Голосование за следующую карту запущено', 'Voting for the next map started', 'Abstimmung über die nächste Karte gestartet', 'Rozpoczęto głosowanie na następną mapę', '[]'::jsonb),
            ('MapRotation.Admin.VoteUnavailable', 'Сейчас нельзя запустить голосование', 'Voting cannot be started right now', 'Die Abstimmung kann derzeit nicht gestartet werden', 'Nie można teraz rozpocząć głosowania', '[]'::jsonb),
            ('MapRotation.Admin.NextMapSet', 'Следующая карта установлена: {map}', 'Next map set: {map}', 'Nächste Karte festgelegt: {map}', 'Ustawiono następną mapę: {map}', '[{"name": "map", "type": "string", "required": true, "description": "Название карты из каталога", "example": "Gorodok"}]'::jsonb),
            ('MapRotation.Admin.InvalidMap', 'Карта не найдена или недоступна', 'Map not found or unavailable', 'Karte nicht gefunden oder nicht verfügbar', 'Nie znaleziono mapy lub jest ona niedostępna', '[]'::jsonb);
        INSERT INTO localization.entries(key, description, parameters, is_critical)
        SELECT key, 'Ротация карт: карточки и ответы команд', parameters, FALSE FROM map_rotation_interface_seed
        ON CONFLICT (key) DO NOTHING;
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT entry.id, language.code, translated.text FROM map_rotation_interface_seed seed
        JOIN localization.entries entry ON entry.key = seed.key
        CROSS JOIN LATERAL (VALUES ('ru', seed.ru), ('en', seed.en), ('de', seed.de), ('pl', seed.pl)) translated(code, text)
        JOIN localization.languages language ON language.code = translated.code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        DROP TABLE map_rotation_interface_seed;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Пользовательские переводы сохраняются при откате.
    }
}
