using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260925120000_AddMapRotationCompactHudLocalization")]
internal sealed class AddMapRotationCompactHudLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TEMP TABLE map_rotation_compact_seed(key text, ru text, en text, de text, pl text, parameters jsonb) ON COMMIT DROP;
        INSERT INTO map_rotation_compact_seed VALUES
            ('MapRotation.Participation', 'Проголосовало {voted} из {total}', 'Voted {voted} of {total}', 'Abgestimmt: {voted} von {total}', 'Zagłosowało {voted} z {total}', '[{"name": "voted", "type": "integer", "required": true, "description": "Количество проголосовавших игроков", "example": "14"}, {"name": "total", "type": "integer", "required": true, "description": "Количество игроков с правом голоса", "example": "24"}]'::jsonb),
            ('MapRotation.VoteStats', '{count} · {percent}%', '{count} · {percent}%', '{count} · {percent}%', '{count} · {percent}%', '[{"name": "count", "type": "integer", "required": true, "description": "Количество голосов за карту", "example": "7"}, {"name": "percent", "type": "integer", "required": true, "description": "Доля от поданных голосов", "example": "50"}]'::jsonb),
            ('MapRotation.VoteFooter', 'Голос можно изменить до окончания голосования', 'You can change your vote until voting ends', 'Du kannst deine Stimme bis zum Ende der Abstimmung ändern', 'Możesz zmienić głos do końca głosowania', '[]'::jsonb),
            ('MapRotation.CompactFooter', 'Открыть голосование: !rtv', 'Open voting: !rtv', 'Abstimmung öffnen: !rtv', 'Otwórz głosowanie: !rtv', '[]'::jsonb),
            ('MapRotation.ResultDismiss', 'Итог скроется через {seconds} с', 'Result closes in {seconds}s', 'Ergebnis schließt in {seconds} s', 'Wynik zniknie za {seconds} s', '[{"name": "seconds", "type": "integer", "required": true, "description": "Оставшееся время показа итога", "example": "6"}]'::jsonb),
            ('MapRotation.Settings.DockSide', 'Сворачивать голосование', 'Minimize voting to', 'Abstimmung minimieren', 'Zwijaj głosowanie', '[]'::jsonb),
            ('MapRotation.Settings.Left', 'Слева', 'Left', 'Links', 'Po lewej', '[]'::jsonb),
            ('MapRotation.Settings.Right', 'Справа', 'Right', 'Rechts', 'Po prawej', '[]'::jsonb),
            ('MapRotation.Settings.Animation', 'Анимация', 'Animation', 'Animation', 'Animacja', '[]'::jsonb),
            ('MapRotation.Settings.AnimationNone', 'Без анимации', 'None', 'Keine', 'Brak', '[]'::jsonb),
            ('MapRotation.Settings.AnimationFast', 'Быстрая', 'Fast', 'Schnell', 'Szybka', '[]'::jsonb),
            ('MapRotation.Settings.AnimationNormal', 'Обычная', 'Normal', 'Normal', 'Normalna', '[]'::jsonb),
            ('MapRotation.Settings.AnimationSlow', 'Плавная', 'Slow', 'Langsam', 'Wolna', '[]'::jsonb);
        INSERT INTO localization.entries(key, description, parameters, is_critical)
        SELECT key, 'Ротация карт: компактное голосование, участие и анимация', parameters, FALSE FROM map_rotation_compact_seed
        ON CONFLICT (key) DO NOTHING;
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT entry.id, language.code, translated.text FROM map_rotation_compact_seed seed
        JOIN localization.entries entry ON entry.key = seed.key
        CROSS JOIN LATERAL (VALUES ('ru', seed.ru), ('en', seed.en), ('de', seed.de), ('pl', seed.pl)) translated(code, text)
        JOIN localization.languages language ON language.code = translated.code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        DROP TABLE map_rotation_compact_seed;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Пользовательские переводы сохраняются при откате.
    }
}
