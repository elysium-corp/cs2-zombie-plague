using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260907210000_AddRoundHudBanner")]
internal sealed class AddRoundHudBanner : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Round.Hud.Started', 'Баннер начала режима раунда, параметр mode — название режима', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Round.Hud.Started'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES
            ('ru', '<font color=''muted''>Раунд начался</font><br><b><font color=''mint''>{mode}</font></b>'),
            ('en', '<font color=''muted''>Round started</font><br><b><font color=''mint''>{mode}</font></b>'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Сохраняем переводы, которые администратор мог изменить на сайте
    }
}
