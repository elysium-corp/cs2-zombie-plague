using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Advertisement.Core.Database.Migrations;

[DbContext(typeof(AdvertisementDbContext))]
[Migration("20260908070000_AddBannerTemplates")]
internal sealed class AddBannerTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        CREATE TABLE advertisement.banner_templates (
            key VARCHAR(64) PRIMARY KEY CHECK (key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$'),
            name VARCHAR(128) NOT NULL CHECK (btrim(name) <> ''),
            design JSONB NOT NULL CHECK (jsonb_typeof(design) = 'object' AND octet_length(design::text) <= 8192),
            sound_preview_url TEXT,
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        ALTER TABLE advertisement.messages
            ALTER COLUMN localization_key DROP NOT NULL,
            ADD COLUMN banner_template_key VARCHAR(64) REFERENCES advertisement.banner_templates(key) ON UPDATE CASCADE ON DELETE RESTRICT,
            ADD COLUMN banner_header_key VARCHAR(191) REFERENCES localization.entries(key) ON UPDATE CASCADE ON DELETE RESTRICT,
            ADD COLUMN banner_title_key VARCHAR(191) REFERENCES localization.entries(key) ON UPDATE CASCADE ON DELETE RESTRICT,
            ADD COLUMN banner_parameters JSONB NOT NULL DEFAULT '{}'::jsonb,
            ADD CONSTRAINT messages_chat_key_required CHECK (display_type = 'hud' OR (localization_key IS NOT NULL AND btrim(localization_key) <> '')),
            ADD CONSTRAINT messages_banner_parameters_valid CHECK (jsonb_typeof(banner_parameters) = 'object' AND octet_length(banner_parameters::text) <= 8192),
            ADD CONSTRAINT messages_banner_fields_valid CHECK (banner_template_key IS NOT NULL OR (banner_header_key IS NULL AND banner_title_key IS NULL));
        CREATE INDEX messages_banner_template_idx ON advertisement.messages(banner_template_key) WHERE banner_template_key IS NOT NULL;
        CREATE INDEX messages_banner_header_idx ON advertisement.messages(banner_header_key) WHERE banner_header_key IS NOT NULL;
        CREATE INDEX messages_banner_title_idx ON advertisement.messages(banner_title_key) WHERE banner_title_key IS NOT NULL;
        UPDATE advertisement.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        DO $$ BEGIN
            IF EXISTS (SELECT 1 FROM advertisement.messages WHERE localization_key IS NULL) THEN
                RAISE EXCEPTION 'Add chat Localization keys to HUD-only messages before rolling back banner templates';
            END IF;
        END $$;
        ALTER TABLE advertisement.messages
            DROP CONSTRAINT messages_chat_key_required,
            DROP CONSTRAINT messages_banner_fields_valid,
            DROP COLUMN banner_template_key,
            DROP COLUMN banner_header_key,
            DROP COLUMN banner_title_key,
            DROP COLUMN banner_parameters,
            ALTER COLUMN localization_key SET NOT NULL;
        DROP TABLE advertisement.banner_templates;
        """);
}
