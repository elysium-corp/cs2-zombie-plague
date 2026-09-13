using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260913195000_AddShopHudTimerLocalization")]
internal sealed class AddShopHudTimerLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TEMP TABLE shop_hud_timer_seed (key VARCHAR(191), ru TEXT, en TEXT) ON COMMIT DROP;
        INSERT INTO shop_hud_timer_seed VALUES ('Shop.Hud.Timer.Seconds', 'с', 's'),
            ('Shop.Hud.Timer.Minutes', 'м', 'm'),
            ('Shop.Hud.Timer.Hours', 'ч', 'h'),
            ('Shop.Hud.Timer.Days', 'д', 'd');
        INSERT INTO localization.entries (key, description, is_critical, parameters)
        SELECT seed.key, 'Настройки HUD магазина', FALSE, '[]'::jsonb FROM shop_hud_timer_seed seed
        WHERE NOT EXISTS (SELECT 1 FROM localization.entries entry WHERE lower(entry.key) = lower(seed.key));
        INSERT INTO localization.translations (entry_id, language_code, text)
        SELECT entry.id, language.code, CASE language.code WHEN 'ru' THEN seed.ru ELSE seed.en END
        FROM shop_hud_timer_seed seed
        JOIN localization.entries entry ON lower(entry.key) = lower(seed.key)
        JOIN localization.languages language ON language.code IN ('ru', 'en')
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW() WHERE id = 1;
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Переводы могли быть изменены администратором; при откате оставляем их.
    }
}
