using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260923010000_AddMapRotationLocalization")]
internal sealed class AddMapRotationLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TEMP TABLE map_rotation_seed(key text, ru text, en text, parameters jsonb) ON COMMIT DROP;
        INSERT INTO map_rotation_seed VALUES
            ('MapRotation.Loading', 'Ротация карт загружается', 'Map rotation is loading', '[]'::jsonb),
            ('MapRotation.TimeLeft', 'До смены карты', 'Time left', '[]'::jsonb),
            ('MapRotation.NextMap', 'Следующая карта', 'Next map', '[]'::jsonb),
            ('MapRotation.LastRound', 'Последний раунд', 'Final round', '[]'::jsonb),
            ('MapRotation.NotSelected', 'Пока не выбрана', 'Not selected yet', '[]'::jsonb),
            ('MapRotation.RtvDelay', 'RTV доступен через', 'RTV available in', '[]'::jsonb),
            ('MapRotation.RtvRemaining', 'Осталось голосов: {count}', '{count} more votes needed', '[{"name": "count", "type": "string", "required": true, "description": "count", "example": "1"}]'::jsonb),
            ('MapRotation.Accepted', 'Сохранено', 'Saved', '[]'::jsonb),
            ('MapRotation.Duplicate', 'Ваш голос уже учтён', 'Your vote has already been counted', '[]'::jsonb),
            ('MapRotation.Disabled', 'Функция отключена', 'Feature disabled', '[]'::jsonb),
            ('MapRotation.TooFewPlayers', 'Недостаточно игроков для RTV', 'Not enough players for RTV', '[]'::jsonb),
            ('MapRotation.NotEligible', 'Вы не можете участвовать', 'You cannot participate', '[]'::jsonb),
            ('MapRotation.Locked', 'Следующая карта уже выбирается или выбрана', 'The next map is being selected or has been selected', '[]'::jsonb),
            ('MapRotation.InvalidMap', 'Карта недоступна', 'Map unavailable', '[]'::jsonb),
            ('MapRotation.NominationTitle', 'Номинация карты', 'Nominate a map', '[]'::jsonb),
            ('MapRotation.NominationSubtitle', 'Выберите карту для следующего голосования', 'Choose a map for the next vote', '[]'::jsonb),
            ('MapRotation.VoteTitle', 'Выберите следующую карту', 'Choose the next map', '[]'::jsonb),
            ('MapRotation.VoteSubtitle', 'Голосование за следующую карту', 'Vote for the next map', '[]'::jsonb),
            ('MapRotation.YourVote', 'Ваш голос', 'Your vote', '[]'::jsonb),
            ('MapRotation.Votes', 'Голосов: {count}', 'Votes: {count}', '[{"name": "count", "type": "string", "required": true, "description": "count", "example": "1"}]'::jsonb),
            ('MapRotation.Close', 'Закрыть', 'Close', '[]'::jsonb),
            ('MapRotation.NoMaps', 'Нет доступных карт', 'No maps available', '[]'::jsonb),
            ('MapRotation.ResultTitle', 'Голосование завершено', 'Voting complete', '[]'::jsonb),
            ('MapRotation.LastRoundDescription', 'Текущий раунд — последний
        Карта сменится после завершения раунда', 'This is the final round
        The map changes when the round ends', '[]'::jsonb),
            ('MapRotation.ScheduledResult', 'Карта сменится после окончания времени и раунда', 'The map changes after the time limit and the final round', '[]'::jsonb),
            ('MapRotation.RtvAdded', 'Игрок {player} поддержал RTV ({votes} / {required})', '{player} requested RTV ({votes} / {required})', '[{"name": "player", "type": "string", "required": true, "description": "player", "example": "1"}, {"name": "votes", "type": "string", "required": true, "description": "votes", "example": "1"}, {"name": "required", "type": "string", "required": true, "description": "required", "example": "1"}]'::jsonb),
            ('MapRotation.VoteStarted', 'Открыто голосование за следующую карту', 'Voting for the next map has started', '[]'::jsonb),
            ('MapRotation.ForcedChange', 'Последний раунд затянулся — выполняется смена карты', 'Final round timeout — changing map', '[]'::jsonb),
            ('MapRotation.HudUnavailable', 'Интерфейс меню временно недоступен', 'Menu interface is temporarily unavailable', '[]'::jsonb);
        INSERT INTO localization.entries(key, description, parameters, is_critical)
        SELECT key, 'Ротация карт и HUD-меню', parameters, FALSE FROM map_rotation_seed
        ON CONFLICT (key) DO NOTHING;
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT entry.id, language.code, translated.text FROM map_rotation_seed seed
        JOIN localization.entries entry ON entry.key = seed.key
        CROSS JOIN LATERAL (VALUES ('ru', seed.ru), ('en', seed.en), ('de', seed.en), ('pl', seed.en)) translated(code, text)
        JOIN localization.languages language ON language.code = translated.code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        DROP TABLE map_rotation_seed;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Пользовательские переводы сохраняются при откате.
    }
}
