using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Advertisement.Core.Database.Migrations;

[DbContext(typeof(AdvertisementDbContext))]
[Migration("20260907220000_AddHudDelivery")]
internal sealed class AddHudDelivery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE advertisement.messages
                ADD COLUMN hud_localization_key VARCHAR(191),
                ADD COLUMN hud_position VARCHAR(16) NOT NULL DEFAULT 'bottom_left',
                ADD COLUMN hud_duration_seconds DOUBLE PRECISION NOT NULL DEFAULT 8,
                ADD COLUMN hud_style VARCHAR(16) NOT NULL DEFAULT 'notice';

            ALTER TABLE advertisement.messages
                DROP CONSTRAINT IF EXISTS messages_display_type_check,
                DROP CONSTRAINT IF EXISTS ck_advertisement_messages_display_type,
                ADD CONSTRAINT ck_advertisement_messages_display_type CHECK (display_type IN ('chat', 'hud', 'chat_and_hud')),
                ADD CONSTRAINT messages_hud_position_valid CHECK (hud_position IN ('top_left', 'top_center', 'top_right', 'middle_left', 'center', 'middle_right', 'bottom_left', 'bottom_center', 'bottom_right')),
                ADD CONSTRAINT messages_hud_duration_valid CHECK (hud_duration_seconds BETWEEN 0.5 AND 60),
                ADD CONSTRAINT messages_hud_style_valid CHECK (hud_style IN ('notice', 'banner')),
                ADD CONSTRAINT messages_hud_key_required CHECK (display_type = 'chat' OR (hud_localization_key IS NOT NULL AND btrim(hud_localization_key) <> '')),
                ADD CONSTRAINT messages_hud_localization_key_fkey FOREIGN KEY (hud_localization_key)
                    REFERENCES localization.entries(key) ON UPDATE CASCADE ON DELETE RESTRICT;

            CREATE INDEX messages_hud_localization_key_idx ON advertisement.messages(hud_localization_key)
                WHERE hud_localization_key IS NOT NULL;
            UPDATE advertisement.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Откат сохраняет сообщения и их чатовый текст, удаляя только настройки нового канала.
        migrationBuilder.Sql(
            """
            UPDATE advertisement.messages SET display_type = 'chat' WHERE display_type <> 'chat';
            ALTER TABLE advertisement.messages
                DROP CONSTRAINT IF EXISTS messages_display_type_check,
                DROP CONSTRAINT IF EXISTS ck_advertisement_messages_display_type,
                ADD CONSTRAINT ck_advertisement_messages_display_type CHECK (display_type = 'chat'),
                DROP COLUMN hud_localization_key,
                DROP COLUMN hud_position,
                DROP COLUMN hud_duration_seconds,
                DROP COLUMN hud_style;
            """);
    }
}
