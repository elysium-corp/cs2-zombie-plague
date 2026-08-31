using Localization.Core.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260831020000_ExpandLocalizationCatalog")]
internal sealed class ExpandLocalizationCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var (key, translations) in BuiltInLocalizationEntries.Create())
        {
            var escapedKey = Escape(key);
            var plugin = Escape(key.Split('.', 2)[0]);

            migrationBuilder.Sql(
                $"""
                INSERT INTO localization.entries (key, description, is_critical)
                VALUES ('{escapedKey}', 'Системный ключ модуля {plugin}', FALSE)
                ON CONFLICT (key) DO NOTHING;
                """);

            foreach (var (language, text) in translations)
            {
                migrationBuilder.Sql(
                    $"""
                    INSERT INTO localization.translations (entry_id, language_code, text)
                    SELECT entry.id, language.code, '{Escape(text)}'
                    FROM localization.entries AS entry
                    JOIN localization.languages AS language ON language.code = '{Escape(language)}'
                    WHERE entry.key = '{escapedKey}'
                    ON CONFLICT (entry_id, language_code) DO NOTHING;
                    """);
            }
        }

        migrationBuilder.Sql(
            """
            UPDATE localization.settings
            SET configuration_version = configuration_version + 1,
                updated_at = NOW()
            WHERE id = 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Каталог мог быть изменён администратором после миграции; данные не удаляем.
    }

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
