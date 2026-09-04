using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260904120000_OwnAdvertisementTags")]
internal sealed class OwnAdvertisementTags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS localization.tags (
                id BIGSERIAL PRIMARY KEY,
                key VARCHAR(64) NOT NULL,
                localization_key VARCHAR(191) NOT NULL
                    REFERENCES localization.entries(key) ON UPDATE CASCADE ON DELETE CASCADE,
                color VARCHAR(32) NOT NULL DEFAULT 'default',
                enabled BOOLEAN NOT NULL DEFAULT TRUE,
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT tags_key_format CHECK (key ~ '^[a-z0-9][a-z0-9_.-]{0,63}$'),
                CONSTRAINT tags_localization_key_group
                    CHECK (lower(localization_key) = lower('Tags.' || key))
            );

            CREATE UNIQUE INDEX IF NOT EXISTS tags_key_unique
                ON localization.tags (key);
            CREATE UNIQUE INDEX IF NOT EXISTS tags_localization_key_unique
                ON localization.tags (localization_key);

            DO $migration$
            BEGIN
                IF to_regclass('advertisement.tags') IS NOT NULL THEN
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
                END IF;
            END
            $migration$;

            INSERT INTO localization.entries (key, description, is_critical, parameters)
            VALUES ('Tags.elysium', 'Основной тег Elysium', FALSE, '[]'::jsonb)
            ON CONFLICT (key) DO UPDATE SET is_critical = FALSE;

            INSERT INTO localization.translations (entry_id, language_code, text)
            SELECT entry.id, language.code, 'Elysium'
            FROM localization.entries AS entry
            JOIN localization.languages AS language
              ON language.code IN ('ru', 'en', 'de', 'pl')
            WHERE lower(entry.key) = lower('Tags.elysium')
            ON CONFLICT (entry_id, language_code) DO NOTHING;

            INSERT INTO localization.tags (key, localization_key, color, enabled, sort_order)
            VALUES ('elysium', 'Tags.elysium', 'purple', TRUE, 0)
            ON CONFLICT (key) DO NOTHING;

            ALTER TABLE IF EXISTS advertisement.messages
                ADD COLUMN IF NOT EXISTS tag_key VARCHAR(64);

            DO $migration$
            BEGIN
                IF to_regclass('advertisement.messages') IS NOT NULL
                   AND to_regclass('advertisement.tags') IS NOT NULL
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
            END
            $migration$;

            ALTER TABLE IF EXISTS advertisement.messages
                DROP COLUMN IF EXISTS tag_id CASCADE;
            DROP TABLE IF EXISTS advertisement.tag_translations;
            DROP TABLE IF EXISTS advertisement.tags;

            DO $migration$
            BEGIN
                IF to_regclass('advertisement.messages') IS NOT NULL THEN
                    CREATE INDEX IF NOT EXISTS messages_tag_key_idx
                        ON advertisement.messages (tag_key)
                        WHERE tag_key IS NOT NULL;

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

            CREATE OR REPLACE FUNCTION localization.bump_configuration_version()
            RETURNS TRIGGER AS $$
            BEGIN
                UPDATE localization.settings
                SET configuration_version = configuration_version + 1,
                    updated_at = NOW()
                WHERE id = 1;
                RETURN NULL;
            END;
            $$ LANGUAGE plpgsql;

            DROP TRIGGER IF EXISTS tags_bump_localization_version ON localization.tags;
            CREATE TRIGGER tags_bump_localization_version
                AFTER INSERT OR UPDATE OR DELETE ON localization.tags
                FOR EACH STATEMENT EXECUTE FUNCTION localization.bump_configuration_version();

            UPDATE localization.settings
            SET configuration_version = configuration_version + 1,
                updated_at = NOW()
            WHERE id = 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE SCHEMA IF NOT EXISTS advertisement;

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
            JOIN advertisement.tags AS advertisement_tag ON advertisement_tag.key = localization_tag.key
            JOIN localization.entries AS entry ON entry.key = localization_tag.localization_key
            JOIN localization.translations AS translation ON translation.entry_id = entry.id
            ON CONFLICT (tag_id, locale) DO UPDATE SET
                text = EXCLUDED.text,
                updated_at = NOW();

            ALTER TABLE IF EXISTS advertisement.messages
                ADD COLUMN IF NOT EXISTS tag_id BIGINT;

            DO $migration$
            BEGIN
                IF to_regclass('advertisement.messages') IS NOT NULL
                   AND EXISTS (
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

            ALTER TABLE IF EXISTS advertisement.messages
                DROP COLUMN IF EXISTS tag_key CASCADE;

            DO $migration$
            BEGIN
                IF to_regclass('advertisement.messages') IS NOT NULL
                   AND NOT EXISTS (
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

            DROP INDEX IF EXISTS advertisement.messages_tag_key_idx;

            DROP TRIGGER IF EXISTS tags_bump_localization_version ON localization.tags;
            DELETE FROM localization.entries AS entry
            USING localization.tags AS tag
            WHERE entry.key = tag.localization_key;
            DROP TABLE IF EXISTS localization.tags;
            """);
    }
}
