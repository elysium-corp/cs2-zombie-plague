using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Advertisement.Core.Database.Migrations;

[DbContext(typeof(AdvertisementDbContext))]
[Migration("20260827080000_InitialAdvertisement")]
internal sealed class InitialAdvertisement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE SCHEMA IF NOT EXISTS advertisement;
            CREATE SCHEMA IF NOT EXISTS core;

            CREATE TABLE IF NOT EXISTS advertisement.settings (
                id BIGSERIAL PRIMARY KEY,
                server_id BIGINT NULL,
                enabled BOOLEAN NOT NULL DEFAULT TRUE,
                default_locale VARCHAR(16) NOT NULL DEFAULT 'ru',
                allowed_locales JSONB NOT NULL DEFAULT '["ru","en","uk","pl","de"]'::jsonb,
                interval_seconds INTEGER NOT NULL DEFAULT 90 CHECK (interval_seconds >= 10),
                refresh_interval_seconds INTEGER NOT NULL DEFAULT 30 CHECK (refresh_interval_seconds >= 5),
                initial_delay_seconds INTEGER NOT NULL DEFAULT 45 CHECK (initial_delay_seconds >= 0),
                order_mode VARCHAR(32) NOT NULL DEFAULT 'sequential'
                    CHECK (order_mode IN ('sequential','random','weighted_random')),
                exclude_bots_from_players BOOLEAN NOT NULL DEFAULT TRUE,
                colors JSONB NOT NULL DEFAULT '{"default":"default","accent":"lightblue","warning":"red","success":"green","important":"orange","muted":"gray"}'::jsonb,
                configuration_version BIGINT NOT NULL DEFAULT 1,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT settings_server_scope_unique UNIQUE NULLS NOT DISTINCT (server_id)
            );

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

            CREATE TABLE IF NOT EXISTS advertisement.messages (
                id BIGSERIAL PRIMARY KEY,
                server_id BIGINT NULL,
                key VARCHAR(64) NOT NULL,
                name VARCHAR(128) NOT NULL,
                tag_id BIGINT NULL REFERENCES advertisement.tags(id) ON DELETE SET NULL,
                type VARCHAR(32) NOT NULL DEFAULT 'information'
                    CHECK (type IN ('information','advertisement','tip','warning','event','system')),
                display_type VARCHAR(32) NOT NULL DEFAULT 'chat' CHECK (display_type = 'chat'),
                enabled BOOLEAN NOT NULL DEFAULT TRUE,
                priority INTEGER NOT NULL DEFAULT 0,
                weight INTEGER NOT NULL DEFAULT 100 CHECK (weight >= 0),
                sort_order INTEGER NOT NULL DEFAULT 0,
                interval_seconds INTEGER NULL CHECK (interval_seconds IS NULL OR interval_seconds >= 10),
                min_players INTEGER NULL CHECK (min_players IS NULL OR min_players >= 0),
                max_players INTEGER NULL CHECK (max_players IS NULL OR max_players >= 0),
                starts_at TIMESTAMPTZ NULL,
                ends_at TIMESTAMPTZ NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT messages_server_key_unique UNIQUE NULLS NOT DISTINCT (server_id, key),
                CONSTRAINT messages_player_range_valid CHECK (min_players IS NULL OR max_players IS NULL OR min_players <= max_players),
                CONSTRAINT messages_time_range_valid CHECK (starts_at IS NULL OR ends_at IS NULL OR starts_at < ends_at)
            );

            CREATE TABLE IF NOT EXISTS advertisement.message_translations (
                message_id BIGINT NOT NULL REFERENCES advertisement.messages(id) ON DELETE CASCADE,
                locale VARCHAR(16) NOT NULL,
                text TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                PRIMARY KEY (message_id, locale)
            );

            CREATE TABLE IF NOT EXISTS core.player_preferences (
                steam_id BIGINT PRIMARY KEY,
                locale VARCHAR(16) NULL,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            ALTER TABLE core.player_preferences ADD COLUMN IF NOT EXISTS locale VARCHAR(16) NULL;
            ALTER TABLE core.player_preferences ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW();

            CREATE INDEX IF NOT EXISTS messages_active_scope_idx
                ON advertisement.messages (server_id, enabled, priority DESC, sort_order, id);
            CREATE INDEX IF NOT EXISTS messages_schedule_idx
                ON advertisement.messages (starts_at, ends_at) WHERE enabled = TRUE;
            CREATE INDEX IF NOT EXISTS message_translations_locale_idx
                ON advertisement.message_translations (locale);
            CREATE INDEX IF NOT EXISTS tag_translations_locale_idx
                ON advertisement.tag_translations (locale);

            INSERT INTO advertisement.settings (server_id)
            SELECT NULL
            WHERE NOT EXISTS (SELECT 1 FROM advertisement.settings WHERE server_id IS NULL);

            INSERT INTO advertisement.tags (key, color, sort_order)
            VALUES ('elysium', 'purple', 0)
            ON CONFLICT (key) DO NOTHING;

            INSERT INTO advertisement.tag_translations (tag_id, locale, text)
            SELECT id, locale, 'Elysium'
            FROM advertisement.tags
            CROSS JOIN (VALUES ('ru'), ('en'), ('uk'), ('pl'), ('de')) AS locales(locale)
            WHERE key = 'elysium'
            ON CONFLICT (tag_id, locale) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP SCHEMA IF EXISTS advertisement CASCADE;");
    }
}
