using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Advertisement.Core.Database.Migrations;

[DbContext(typeof(AdvertisementDbContext))]
[Migration("20260831030000_ReferenceLocalizationKeys")]
internal sealed class ReferenceLocalizationKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE advertisement.messages
                ADD COLUMN IF NOT EXISTS localization_key VARCHAR(191);

            UPDATE advertisement.messages
            SET localization_key = 'advertisement.messages.' || key
            WHERE localization_key IS NULL OR btrim(localization_key) = '';

            ALTER TABLE advertisement.messages
                ALTER COLUMN localization_key SET NOT NULL;

            CREATE INDEX IF NOT EXISTS messages_localization_key_idx
                ON advertisement.messages (localization_key);

            DO $migration$
            BEGIN
                IF to_regclass('localization.entries') IS NOT NULL THEN
                    INSERT INTO localization.entries (key, description)
                    SELECT message.localization_key, message.name
                    FROM advertisement.messages AS message
                    ON CONFLICT (key) DO NOTHING;

                    IF NOT EXISTS (
                       SELECT 1
                       FROM pg_constraint
                       WHERE conname = 'messages_localization_key_fkey'
                         AND conrelid = 'advertisement.messages'::regclass
                    ) THEN
                        ALTER TABLE advertisement.messages
                            ADD CONSTRAINT messages_localization_key_fkey
                            FOREIGN KEY (localization_key)
                            REFERENCES localization.entries(key)
                            ON UPDATE CASCADE
                            ON DELETE RESTRICT;
                    END IF;
                END IF;
            END
            $migration$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE advertisement.messages
                DROP CONSTRAINT IF EXISTS messages_localization_key_fkey;
            DROP INDEX IF EXISTS advertisement.messages_localization_key_idx;
            ALTER TABLE advertisement.messages
                DROP COLUMN IF EXISTS localization_key;
            """);
    }
}
