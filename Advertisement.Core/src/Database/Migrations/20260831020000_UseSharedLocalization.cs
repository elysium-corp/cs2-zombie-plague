using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Advertisement.Core.Database.Migrations;

[DbContext(typeof(AdvertisementDbContext))]
[Migration("20260831020000_UseSharedLocalization")]
internal sealed class UseSharedLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $migration$
            BEGIN
                IF to_regclass('localization.entries') IS NULL
                   OR to_regclass('localization.translations') IS NULL
                   OR to_regclass('localization.languages') IS NULL THEN
                    RETURN;
                END IF;

                INSERT INTO localization.entries (key, description)
                SELECT 'advertisement.messages.' || message.key, message.name
                FROM advertisement.messages AS message
                ON CONFLICT (key) DO UPDATE
                    SET description = COALESCE(EXCLUDED.description, localization.entries.description);

                INSERT INTO localization.translations (entry_id, language_code, text)
                SELECT entry.id, language.code, translation.text
                FROM advertisement.message_translations AS translation
                JOIN advertisement.messages AS message ON message.id = translation.message_id
                JOIN localization.entries AS entry
                  ON entry.key = 'advertisement.messages.' || message.key
                JOIN localization.languages AS language
                  ON language.code = lower(translation.locale)
                ON CONFLICT (entry_id, language_code) DO UPDATE SET
                    text = EXCLUDED.text,
                    updated_at = NOW();

                INSERT INTO localization.entries (key, description)
                SELECT 'advertisement.tags.' || tag.key, 'Тег рекламы ' || tag.key
                FROM advertisement.tags AS tag
                ON CONFLICT (key) DO NOTHING;

                INSERT INTO localization.translations (entry_id, language_code, text)
                SELECT entry.id, language.code, translation.text
                FROM advertisement.tag_translations AS translation
                JOIN advertisement.tags AS tag ON tag.id = translation.tag_id
                JOIN localization.entries AS entry
                  ON entry.key = 'advertisement.tags.' || tag.key
                JOIN localization.languages AS language
                  ON language.code = lower(translation.locale)
                ON CONFLICT (entry_id, language_code) DO UPDATE SET
                    text = EXCLUDED.text,
                    updated_at = NOW();
            END
            $migration$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Старые таблицы переводов не удаляются, поэтому откат не требует SQL.
    }
}
