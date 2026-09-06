using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260906130000_RenameClericRussianTranslation")]
internal sealed class RenameClericRussianTranslation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        UPDATE localization.translations AS translation
        SET text = 'Клерик'
        FROM localization.entries AS entry
        WHERE translation.entry_id = entry.id
          AND lower(entry.key) = lower('ZombiePlague.ZClass.Zombie.Cleric.Name')
          AND lower(translation.language_code) = 'ru'
          AND translation.text = 'Клирик';
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Пользовательские переводы не откатываем при понижении версии плагина
    }
}
