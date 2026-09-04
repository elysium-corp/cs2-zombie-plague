using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Advertisement.Core.Database.Migrations;

[DbContext(typeof(AdvertisementDbContext))]
[Migration("20260904121000_ReferenceLocalizationTags")]
internal sealed class ReferenceLocalizationTags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE advertisement.messages
                ADD COLUMN IF NOT EXISTS tag_key VARCHAR(64);

            DO $migration$
            BEGIN
                IF to_regclass('advertisement.tags') IS NOT NULL
                   AND EXISTS (
                       SELECT 1
                       FROM information_schema.columns
                       WHERE table_schema = 'advertisement'
                         AND table_name = 'messages'
                         AND column_name = 'tag_id'
                   ) THEN
                    UPDATE advertisement.messages AS message
                    SET tag_key = lower(tag.key)
                    FROM advertisement.tags AS tag
                    WHERE tag.id = message.tag_id
                      AND (message.tag_key IS NULL OR btrim(message.tag_key) = '');
                END IF;

                IF to_regclass('localization.tags') IS NOT NULL
                   AND to_regclass('advertisement.tags') IS NOT NULL THEN
                    INSERT INTO localization.entries (key, description, is_critical, parameters)
                    SELECT 'Tags.' || lower(tag.key), 'Тег ' || tag.key, FALSE, '[]'::jsonb
                    FROM advertisement.tags AS tag
                    ON CONFLICT (key) DO UPDATE SET
                        is_critical = FALSE,
                        updated_at = NOW();

                    IF to_regclass('advertisement.tag_translations') IS NOT NULL THEN
                        INSERT INTO localization.translations (entry_id, language_code, text)
                        SELECT entry.id, language.code, translation.text
                        FROM advertisement.tag_translations AS translation
                        JOIN advertisement.tags AS tag ON tag.id = translation.tag_id
                        JOIN localization.entries AS entry
                          ON lower(entry.key) = lower('Tags.' || tag.key)
                        JOIN localization.languages AS language
                          ON lower(language.code) = lower(translation.locale)
                        ON CONFLICT (entry_id, language_code) DO UPDATE SET
                            text = EXCLUDED.text,
                            updated_at = NOW();
                    END IF;

                    INSERT INTO localization.tags
                        (key, localization_key, color, enabled, sort_order, created_at, updated_at)
                    SELECT lower(tag.key), 'Tags.' || lower(tag.key), lower(tag.color),
                           tag.enabled, tag.sort_order, tag.created_at, tag.updated_at
                    FROM advertisement.tags AS tag
                    ON CONFLICT (key) DO UPDATE SET
                        localization_key = EXCLUDED.localization_key,
                        color = EXCLUDED.color,
                        enabled = EXCLUDED.enabled,
                        sort_order = EXCLUDED.sort_order,
                        updated_at = NOW();

                    ALTER TABLE advertisement.messages
                        DROP COLUMN IF EXISTS tag_id CASCADE;
                    DROP TABLE IF EXISTS advertisement.tag_translations;
                    DROP TABLE IF EXISTS advertisement.tags;
                END IF;

                IF to_regclass('localization.tags') IS NOT NULL THEN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'messages_tag_key_fkey'
                          AND conrelid = 'advertisement.messages'::regclass
                    ) THEN
                        ALTER TABLE advertisement.messages
                            ADD CONSTRAINT messages_tag_key_fkey
                            FOREIGN KEY (tag_key)
                            REFERENCES localization.tags(key)
                            ON UPDATE CASCADE
                            ON DELETE SET NULL;
                    END IF;
                END IF;
            END
            $migration$;

            CREATE INDEX IF NOT EXISTS messages_tag_key_idx
                ON advertisement.messages (tag_key)
                WHERE tag_key IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS advertisement.tags (
                id BIGSERIAL PRIMARY KEY,
                key VARCHAR(64) NOT NULL UNIQUE,
                color VARCHAR(32) NOT NULL DEFAULT 'default',
                enabled BOOLEAN NOT NULL DEFAULT TRUE,
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS advertisement.tag_translations (
                tag_id BIGINT NOT NULL REFERENCES advertisement.tags(id) ON DELETE CASCADE,
                locale VARCHAR(16) NOT NULL,
                text VARCHAR(64) NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                PRIMARY KEY (tag_id, locale)
            );

            CREATE INDEX IF NOT EXISTS tag_translations_locale_idx
                ON advertisement.tag_translations (locale);

            DO $migration$
            BEGIN
                IF to_regclass('localization.tags') IS NOT NULL THEN
                    INSERT INTO advertisement.tags (key, color, enabled, sort_order, created_at, updated_at)
                    SELECT key, color, enabled, sort_order, created_at, updated_at
                    FROM localization.tags
                    ON CONFLICT (key) DO UPDATE SET
                        color = EXCLUDED.color,
                        enabled = EXCLUDED.enabled,
                        sort_order = EXCLUDED.sort_order,
                        updated_at = EXCLUDED.updated_at;

                    INSERT INTO advertisement.tag_translations (tag_id, locale, text)
                    SELECT advertisement_tag.id, translation.language_code, translation.text
                    FROM localization.tags AS localization_tag
                    JOIN advertisement.tags AS advertisement_tag
                      ON advertisement_tag.key = localization_tag.key
                    JOIN localization.entries AS entry
                      ON entry.key = localization_tag.localization_key
                    JOIN localization.translations AS translation
                      ON translation.entry_id = entry.id
                    ON CONFLICT (tag_id, locale) DO UPDATE SET
                        text = EXCLUDED.text,
                        updated_at = NOW();
                END IF;
            END
            $migration$;

            ALTER TABLE advertisement.messages
                ADD COLUMN IF NOT EXISTS tag_id BIGINT;

            DO $migration$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = 'advertisement'
                      AND table_name = 'messages'
                      AND column_name = 'tag_key'
                ) THEN
                    UPDATE advertisement.messages AS message
                    SET tag_id = tag.id
                    FROM advertisement.tags AS tag
                    WHERE lower(tag.key) = lower(message.tag_key);
                END IF;
            END
            $migration$;

            DROP INDEX IF EXISTS advertisement.messages_tag_key_idx;
            ALTER TABLE advertisement.messages
                DROP COLUMN IF EXISTS tag_key CASCADE;

            DO $migration$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'messages_tag_id_fkey'
                      AND conrelid = 'advertisement.messages'::regclass
                ) THEN
                    ALTER TABLE advertisement.messages
                        ADD CONSTRAINT messages_tag_id_fkey
                        FOREIGN KEY (tag_id)
                        REFERENCES advertisement.tags(id)
                        ON DELETE SET NULL;
                END IF;
            END
            $migration$;
            """);
    }
}
