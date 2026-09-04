using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260904090000_RemoveAdvertisementTagLocalizationEntries")]
internal sealed class RemoveAdvertisementTagLocalizationEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $migration$
            BEGIN
                IF to_regclass('advertisement.tags') IS NOT NULL
                   AND to_regclass('advertisement.tag_translations') IS NOT NULL THEN
                    INSERT INTO advertisement.tag_translations (tag_id, locale, text)
                    SELECT tag.id, lower(translation.language_code), translation.text
                    FROM localization.entries AS entry
                    JOIN localization.translations AS translation ON translation.entry_id = entry.id
                    JOIN advertisement.tags AS tag
                      ON lower(entry.key) = lower('advertisement.tags.' || tag.key)
                    WHERE lower(entry.key) LIKE 'advertisement.tags.%'
                    ON CONFLICT (tag_id, locale) DO UPDATE SET
                        text = EXCLUDED.text,
                        updated_at = NOW();
                END IF;
            END
            $migration$;

            DELETE FROM localization.entries
            WHERE lower(key) LIKE 'advertisement.tags.%';

            UPDATE localization.settings
            SET configuration_version = configuration_version + 1,
                updated_at = NOW()
            WHERE id = 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Теги принадлежат advertisement.tag_translations; дубли в Localization не восстанавливаем.
    }
}
