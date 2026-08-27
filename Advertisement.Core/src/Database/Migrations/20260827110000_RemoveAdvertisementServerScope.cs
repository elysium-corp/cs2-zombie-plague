using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Advertisement.Core.Database.Migrations;

[DbContext(typeof(AdvertisementDbContext))]
[Migration("20260827110000_RemoveAdvertisementServerScope")]
internal sealed class RemoveAdvertisementServerScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            LOCK TABLE advertisement.settings, advertisement.messages IN ACCESS EXCLUSIVE MODE;

            DELETE FROM advertisement.settings
            WHERE id <> (
                SELECT id
                FROM advertisement.settings
                ORDER BY (server_id IS NULL) DESC, id
                LIMIT 1
            );

            WITH ranked_messages AS (
                SELECT
                    id,
                    ROW_NUMBER() OVER (
                        PARTITION BY key
                        ORDER BY (server_id IS NULL) DESC, id
                    ) AS row_number
                FROM advertisement.messages
            )
            DELETE FROM advertisement.messages
            WHERE id IN (
                SELECT id
                FROM ranked_messages
                WHERE row_number > 1
            );

            ALTER TABLE advertisement.settings
                DROP CONSTRAINT IF EXISTS settings_server_scope_unique;
            ALTER TABLE advertisement.messages
                DROP CONSTRAINT IF EXISTS messages_server_key_unique;

            DROP INDEX IF EXISTS advertisement.messages_active_scope_idx;

            ALTER TABLE advertisement.settings
                DROP COLUMN IF EXISTS server_id;
            ALTER TABLE advertisement.messages
                DROP COLUMN IF EXISTS server_id;

            ALTER TABLE advertisement.messages
                ADD CONSTRAINT messages_key_unique UNIQUE (key);

            CREATE UNIQUE INDEX settings_singleton_unique
                ON advertisement.settings ((1));
            CREATE INDEX messages_active_idx
                ON advertisement.messages (enabled, priority DESC, sort_order, id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS advertisement.settings_singleton_unique;
            DROP INDEX IF EXISTS advertisement.messages_active_idx;

            ALTER TABLE advertisement.messages
                DROP CONSTRAINT IF EXISTS messages_key_unique;

            ALTER TABLE advertisement.settings
                ADD COLUMN IF NOT EXISTS server_id BIGINT NULL;
            ALTER TABLE advertisement.messages
                ADD COLUMN IF NOT EXISTS server_id BIGINT NULL;

            ALTER TABLE advertisement.settings
                ADD CONSTRAINT settings_server_scope_unique
                UNIQUE NULLS NOT DISTINCT (server_id);
            ALTER TABLE advertisement.messages
                ADD CONSTRAINT messages_server_key_unique
                UNIQUE NULLS NOT DISTINCT (server_id, key);

            CREATE INDEX messages_active_scope_idx
                ON advertisement.messages (server_id, enabled, priority DESC, sort_order, id);
            """);
    }
}
