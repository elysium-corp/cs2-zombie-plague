using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260904150000_NormalizeKeysAndTrackFallback")]
internal sealed class NormalizeKeysAndTrackFallback : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE localization.settings
                ADD COLUMN IF NOT EXISTS fallback_exported_version BIGINT NOT NULL DEFAULT 0;

            ALTER TABLE localization.settings
                DROP CONSTRAINT IF EXISTS settings_fallback_exported_version_valid;
            ALTER TABLE localization.settings
                ADD CONSTRAINT settings_fallback_exported_version_valid
                CHECK (fallback_exported_version >= 0);

            CREATE OR REPLACE FUNCTION localization.canonicalize_key(source TEXT)
            RETURNS TEXT
            LANGUAGE SQL
            IMMUTABLE
            STRICT
            PARALLEL SAFE
            AS $function$
                SELECT string_agg(
                    CASE
                        WHEN part.ordinality = 1 AND lower(part.value) = 'tags' THEN 'Tag'
                        ELSE upper(left(part.value, 1)) || substr(part.value, 2)
                    END,
                    '.' ORDER BY part.ordinality
                )
                FROM unnest(regexp_split_to_array(btrim(source), '[._[:space:]-]+'))
                    WITH ORDINALITY AS part(value, ordinality)
                WHERE part.value <> ''
            $function$;

            ALTER TABLE IF EXISTS localization.tags
                DROP CONSTRAINT IF EXISTS tags_key_format;
            ALTER TABLE IF EXISTS localization.tags
                DROP CONSTRAINT IF EXISTS tags_localization_key_group;
            ALTER TABLE localization.entries
                DROP CONSTRAINT IF EXISTS entries_key_format;

            DO $migration$
            DECLARE
                duplicate_key TEXT;
            BEGIN
                SELECT lower(localization.canonicalize_key(entry.key))
                INTO duplicate_key
                FROM localization.entries AS entry
                GROUP BY lower(localization.canonicalize_key(entry.key))
                HAVING count(*) > 1
                ORDER BY 1
                LIMIT 1;

                IF duplicate_key IS NOT NULL THEN
                    RAISE EXCEPTION
                        'Нельзя нормализовать Localization: несколько записей превращаются в ключ %',
                        duplicate_key;
                END IF;

                IF to_regclass('localization.tags') IS NOT NULL THEN
                    SELECT lower(localization.canonicalize_key(tag.key))
                    INTO duplicate_key
                    FROM localization.tags AS tag
                    GROUP BY lower(localization.canonicalize_key(tag.key))
                    HAVING count(*) > 1
                    ORDER BY 1
                    LIMIT 1;

                    IF duplicate_key IS NOT NULL THEN
                        RAISE EXCEPTION
                            'Нельзя нормализовать теги Localization: несколько тегов превращаются в ключ %',
                            duplicate_key;
                    END IF;
                END IF;

                IF to_regclass('advertisement.messages') IS NOT NULL THEN
                    SELECT lower(localization.canonicalize_key(entry.key))
                    INTO duplicate_key
                    FROM localization.entries AS entry
                    JOIN advertisement.messages AS message
                      ON lower(localization.canonicalize_key(entry.key)) =
                         lower(localization.canonicalize_key(message.key))
                    ORDER BY 1
                    LIMIT 1;

                    IF duplicate_key IS NOT NULL THEN
                        RAISE EXCEPTION
                            'Ключ % одновременно используется в Localization и Advertisement',
                            duplicate_key;
                    END IF;
                END IF;
            END
            $migration$;

            UPDATE localization.entries
            SET key = localization.canonicalize_key(key),
                updated_at = NOW()
            WHERE key <> localization.canonicalize_key(key);

            UPDATE localization.tags
            SET key = localization.canonicalize_key(key),
                localization_key = 'Tag.' || localization.canonicalize_key(key),
                updated_at = NOW()
            WHERE key <> localization.canonicalize_key(key)
               OR localization_key <> 'Tag.' || localization.canonicalize_key(key);

            ALTER TABLE localization.entries
                ADD CONSTRAINT entries_key_format
                CHECK (key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$');

            CREATE UNIQUE INDEX IF NOT EXISTS entries_key_ci_unique
                ON localization.entries (lower(key));

            ALTER TABLE localization.tags
                ADD CONSTRAINT tags_key_format
                CHECK (key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$');
            ALTER TABLE localization.tags
                ADD CONSTRAINT tags_localization_key_group
                CHECK (localization_key = 'Tag.' || key);

            CREATE UNIQUE INDEX IF NOT EXISTS tags_key_ci_unique
                ON localization.tags (lower(key));
            CREATE UNIQUE INDEX IF NOT EXISTS tags_localization_key_ci_unique
                ON localization.tags (lower(localization_key));

            CREATE OR REPLACE FUNCTION localization.reject_entry_key_change()
            RETURNS TRIGGER
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                IF NEW.key IS DISTINCT FROM OLD.key THEN
                    RAISE EXCEPTION 'Ключ Localization нельзя изменять после создания';
                END IF;
                RETURN NEW;
            END
            $function$;

            DROP TRIGGER IF EXISTS entries_key_immutable ON localization.entries;
            CREATE TRIGGER entries_key_immutable
                BEFORE UPDATE OF key ON localization.entries
                FOR EACH ROW EXECUTE FUNCTION localization.reject_entry_key_change();

            CREATE OR REPLACE FUNCTION localization.reject_tag_key_change()
            RETURNS TRIGGER
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                IF NEW.key IS DISTINCT FROM OLD.key
                   OR NEW.localization_key IS DISTINCT FROM OLD.localization_key THEN
                    RAISE EXCEPTION 'Ключ тега Localization нельзя изменять после создания';
                END IF;
                RETURN NEW;
            END
            $function$;

            DROP TRIGGER IF EXISTS tags_key_immutable ON localization.tags;
            CREATE TRIGGER tags_key_immutable
                BEFORE UPDATE OF key, localization_key ON localization.tags
                FOR EACH ROW EXECUTE FUNCTION localization.reject_tag_key_change();

            CREATE OR REPLACE FUNCTION localization.reject_advertisement_key_collision()
            RETURNS TRIGGER
            LANGUAGE plpgsql
            AS $function$
            DECLARE
                collision_exists BOOLEAN;
            BEGIN
                IF to_regclass('advertisement.messages') IS NULL THEN
                    RETURN NEW;
                END IF;

                PERFORM pg_advisory_xact_lock(
                    hashtext('localization-advertisement-key'),
                    hashtext(lower(NEW.key))
                );

                EXECUTE
                    'SELECT EXISTS (SELECT 1 FROM advertisement.messages WHERE lower(key) = lower($1))'
                    INTO collision_exists
                    USING NEW.key;

                IF collision_exists THEN
                    RAISE EXCEPTION
                        'Ключ % уже используется в Advertisement',
                        NEW.key;
                END IF;
                RETURN NEW;
            END
            $function$;

            DROP TRIGGER IF EXISTS entries_key_advertisement_unique ON localization.entries;
            CREATE TRIGGER entries_key_advertisement_unique
                BEFORE INSERT OR UPDATE OF key ON localization.entries
                FOR EACH ROW EXECUTE FUNCTION localization.reject_advertisement_key_collision();

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
            DROP TRIGGER IF EXISTS entries_key_advertisement_unique ON localization.entries;
            DROP FUNCTION IF EXISTS localization.reject_advertisement_key_collision();
            DROP TRIGGER IF EXISTS tags_key_immutable ON localization.tags;
            DROP FUNCTION IF EXISTS localization.reject_tag_key_change();
            DROP TRIGGER IF EXISTS entries_key_immutable ON localization.entries;
            DROP FUNCTION IF EXISTS localization.reject_entry_key_change();

            DROP INDEX IF EXISTS localization.tags_localization_key_ci_unique;
            DROP INDEX IF EXISTS localization.tags_key_ci_unique;
            DROP INDEX IF EXISTS localization.entries_key_ci_unique;

            ALTER TABLE localization.tags
                DROP CONSTRAINT IF EXISTS tags_localization_key_group;
            ALTER TABLE localization.tags
                DROP CONSTRAINT IF EXISTS tags_key_format;
            ALTER TABLE localization.entries
                DROP CONSTRAINT IF EXISTS entries_key_format;

            UPDATE localization.entries AS entry
            SET key = 'Tags.' || lower(tag.key),
                updated_at = NOW()
            FROM localization.tags AS tag
            WHERE entry.key = tag.localization_key;

            UPDATE localization.tags
            SET key = lower(key),
                localization_key = 'Tags.' || lower(key),
                updated_at = NOW();

            ALTER TABLE localization.entries
                ADD CONSTRAINT entries_key_format
                CHECK (key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$');
            ALTER TABLE localization.tags
                ADD CONSTRAINT tags_key_format
                CHECK (key ~ '^[a-z0-9][a-z0-9_.-]{0,63}$');
            ALTER TABLE localization.tags
                ADD CONSTRAINT tags_localization_key_group
                CHECK (lower(localization_key) = lower('Tags.' || key));

            ALTER TABLE localization.settings
                DROP CONSTRAINT IF EXISTS settings_fallback_exported_version_valid;
            ALTER TABLE localization.settings
                DROP COLUMN IF EXISTS fallback_exported_version;

            DROP FUNCTION IF EXISTS localization.canonicalize_key(TEXT);
            """);
    }
}
