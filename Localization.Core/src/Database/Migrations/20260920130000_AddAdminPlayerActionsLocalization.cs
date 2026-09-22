using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260920130000_AddAdminPlayerActionsLocalization")]
internal sealed class AddAdminPlayerActionsLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TEMP TABLE admin_actions_seed(key text, ru text, en text, parameters jsonb) ON COMMIT DROP;
        INSERT INTO admin_actions_seed VALUES
            ('Admin.Actions.Money', 'Выдать деньги', 'Give money', '[]'::jsonb),
            ('Admin.Actions.Noclip', 'Управление noclip', 'Manage noclip', '[]'::jsonb),
            ('Admin.Actions.Grab', 'Захватить игрока (Grab)', 'Grab player', '[]'::jsonb),
            ('Admin.Actions.Mute', 'Заблокировать голос (Mute)', 'Mute voice', '[]'::jsonb),
            ('Admin.Actions.Gag', 'Заблокировать чат (Gag)', 'Gag chat', '[]'::jsonb),
            ('Admin.Actions.Release', 'Отпустить игрока', 'Release player', '[]'::jsonb),
            ('Admin.Actions.Enable', 'Включить', 'Enable', '[]'::jsonb),
            ('Admin.Actions.Disable', 'Выключить', 'Disable', '[]'::jsonb),
            ('Admin.Actions.Minutes', '{amount} мин.', '{amount} min.', '[{"name": "amount", "type": "integer", "required": true, "description": "Количество", "example": "30"}]'::jsonb),
            ('Admin.Actions.Permanent', 'Бессрочно', 'Permanent', '[]'::jsonb),
            ('Admin.Actions.Remove', 'Снять блокировку', 'Remove restriction', '[]'::jsonb),
            ('Admin.Actions.Denied', 'Недостаточно прав.', 'Permission denied.', '[]'::jsonb),
            ('Admin.Actions.TargetMissing', 'Игрок не найден или имя неоднозначно. Используйте #слот или SteamID64.', 'Player not found or name is ambiguous. Use #slot or SteamID64.', '[]'::jsonb),
            ('Admin.Actions.Failed', 'Действие не выполнено. Проверьте состояние игрока, доступность экономики и лимит баланса.', 'Action failed. Check player state, economy availability and balance limit.', '[]'::jsonb),
            ('Admin.Actions.Success', 'Действие выполнено.', 'Action completed.', '[]'::jsonb),
            ('Admin.Actions.Unavailable', 'Хранилище блокировок недоступно или параметры некорректны.', 'Restriction storage is unavailable or parameters are invalid.', '[]'::jsonb),
            ('Admin.Actions.MoneyGranted', 'Начислено: {amount}.', 'Credited: {amount}.', '[{"name": "amount", "type": "integer", "required": true, "description": "Количество", "example": "30"}]'::jsonb),
            ('Admin.Actions.InvalidArguments', 'Неверные параметры: деньги > 0; noclip 0/1; срок 0–525600 минут (0 — бессрочно); причина до 256 символов.', 'Invalid arguments: money > 0; noclip 0/1; duration 0–525600 minutes (0 = permanent); reason up to 256 characters.', '[]'::jsonb);
        INSERT INTO localization.entries(key, description, parameters, is_critical)
        SELECT key, 'Действия администратора над игроками', parameters, FALSE FROM admin_actions_seed
        ON CONFLICT (key) DO NOTHING;
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT entry.id, translated.code, translated.text
        FROM admin_actions_seed seed JOIN localization.entries entry ON entry.key = seed.key
        CROSS JOIN LATERAL (VALUES ('ru', seed.ru), ('en', seed.en)) translated(code, text)
        JOIN localization.languages language ON language.code = translated.code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        DROP TABLE admin_actions_seed;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Сохраняем пользовательские переводы при откате.
    }
}
