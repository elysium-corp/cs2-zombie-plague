using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Advertisement.Core.Database.Migrations;

[DbContext(typeof(AdvertisementDbContext))]
[Migration("20260904151000_NormalizeKeysAndTrackFallback")]
internal sealed class NormalizeKeysAndTrackFallback : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE advertisement.settings
                ADD COLUMN IF NOT EXISTS fallback_exported_version BIGINT NOT NULL DEFAULT 0;

            ALTER TABLE advertisement.settings
                DROP CONSTRAINT IF EXISTS ck_advertisement_settings_fallback_exported_version;
            ALTER TABLE advertisement.settings
                ADD CONSTRAINT ck_advertisement_settings_fallback_exported_version
                CHECK (fallback_exported_version >= 0);

            CREATE OR REPLACE FUNCTION advertisement.canonicalize_key(source TEXT)
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

            ALTER TABLE advertisement.messages
                DROP CONSTRAINT IF EXISTS ck_advertisement_messages_key_format;

            DO $migration$
            DECLARE
                duplicate_key TEXT;
            BEGIN
                SELECT lower(advertisement.canonicalize_key(message.key))
                INTO duplicate_key
                FROM advertisement.messages AS message
                GROUP BY lower(advertisement.canonicalize_key(message.key))
                HAVING count(*) > 1
                ORDER BY 1
                LIMIT 1;

                IF duplicate_key IS NOT NULL THEN
                    RAISE EXCEPTION
                        'Нельзя нормализовать Advertisement: несколько сообщений превращаются в ключ %',
                        duplicate_key;
                END IF;

                IF to_regclass('localization.entries') IS NOT NULL THEN
                    SELECT lower(advertisement.canonicalize_key(message.key))
                    INTO duplicate_key
                    FROM advertisement.messages AS message
                    JOIN localization.entries AS entry
                      ON lower(entry.key) = lower(advertisement.canonicalize_key(message.key))
                    ORDER BY 1
                    LIMIT 1;

                    IF duplicate_key IS NOT NULL THEN
                        RAISE EXCEPTION
                            'Ключ % одновременно используется в Advertisement и Localization',
                            duplicate_key;
                    END IF;
                END IF;
            END
            $migration$;

            UPDATE advertisement.messages
            SET key = advertisement.canonicalize_key(key),
                updated_at = NOW()
            WHERE key <> advertisement.canonicalize_key(key);

            ALTER TABLE advertisement.messages
                ADD CONSTRAINT ck_advertisement_messages_key_format
                CHECK (key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$');

            CREATE UNIQUE INDEX IF NOT EXISTS messages_key_ci_unique
                ON advertisement.messages (lower(key));

            CREATE OR REPLACE FUNCTION advertisement.reject_message_key_change()
            RETURNS TRIGGER
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                IF NEW.key IS DISTINCT FROM OLD.key THEN
                    RAISE EXCEPTION 'Ключ Advertisement нельзя изменять после создания';
                END IF;
                RETURN NEW;
            END
            $function$;

            DROP TRIGGER IF EXISTS messages_key_immutable ON advertisement.messages;
            CREATE TRIGGER messages_key_immutable
                BEFORE UPDATE OF key ON advertisement.messages
                FOR EACH ROW EXECUTE FUNCTION advertisement.reject_message_key_change();

            CREATE OR REPLACE FUNCTION advertisement.reject_localization_key_collision()
            RETURNS TRIGGER
            LANGUAGE plpgsql
            AS $function$
            DECLARE
                collision_exists BOOLEAN;
            BEGIN
                IF to_regclass('localization.entries') IS NULL THEN
                    RETURN NEW;
                END IF;

                PERFORM pg_advisory_xact_lock(
                    hashtext('localization-advertisement-key'),
                    hashtext(lower(NEW.key))
                );

                EXECUTE
                    'SELECT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower($1))'
                    INTO collision_exists
                    USING NEW.key;

                IF collision_exists THEN
                    RAISE EXCEPTION
                        'Ключ % уже используется в Localization',
                        NEW.key;
                END IF;
                RETURN NEW;
            END
            $function$;

            DROP TRIGGER IF EXISTS messages_key_localization_unique ON advertisement.messages;
            CREATE TRIGGER messages_key_localization_unique
                BEFORE INSERT OR UPDATE OF key ON advertisement.messages
                FOR EACH ROW EXECUTE FUNCTION advertisement.reject_localization_key_collision();

            CREATE OR REPLACE FUNCTION advertisement.bump_configuration_version()
            RETURNS TRIGGER
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                UPDATE advertisement.settings
                SET configuration_version = configuration_version + 1,
                    updated_at = NOW();
                RETURN NULL;
            END
            $function$;

            DROP TRIGGER IF EXISTS messages_bump_configuration_version ON advertisement.messages;
            CREATE TRIGGER messages_bump_configuration_version
                AFTER INSERT OR UPDATE OR DELETE ON advertisement.messages
                FOR EACH STATEMENT EXECUTE FUNCTION advertisement.bump_configuration_version();

            UPDATE advertisement.settings
            SET configuration_version = configuration_version + 1,
                updated_at = NOW();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS messages_bump_configuration_version ON advertisement.messages;
            DROP FUNCTION IF EXISTS advertisement.bump_configuration_version();
            DROP TRIGGER IF EXISTS messages_key_localization_unique ON advertisement.messages;
            DROP FUNCTION IF EXISTS advertisement.reject_localization_key_collision();
            DROP TRIGGER IF EXISTS messages_key_immutable ON advertisement.messages;
            DROP FUNCTION IF EXISTS advertisement.reject_message_key_change();

            DROP INDEX IF EXISTS advertisement.messages_key_ci_unique;
            ALTER TABLE advertisement.messages
                DROP CONSTRAINT IF EXISTS ck_advertisement_messages_key_format;

            ALTER TABLE advertisement.settings
                DROP CONSTRAINT IF EXISTS ck_advertisement_settings_fallback_exported_version;
            ALTER TABLE advertisement.settings
                DROP COLUMN IF EXISTS fallback_exported_version;

            DROP FUNCTION IF EXISTS advertisement.canonicalize_key(TEXT);
            """);
    }
}
