using Microsoft.EntityFrameworkCore.Migrations;

namespace MapRotation.Core.Database.Migrations;

/// <summary>Добавляет локализованный заголовок карты и личные настройки компактного голосования.</summary>
public partial class AddMapRotationCompactHud : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // CMS может добавить необязательный ключ до установки обновления игрового сервера.
        migrationBuilder.Sql("""
            ALTER TABLE map_rotation.maps ADD COLUMN IF NOT EXISTS display_name_key character varying(191) NULL;
            ALTER TABLE map_rotation.player_preferences ADD COLUMN dock_side character varying(5) NOT NULL DEFAULT 'right';
            ALTER TABLE map_rotation.player_preferences ADD COLUMN animation character varying(6) NOT NULL DEFAULT 'normal';
            ALTER TABLE map_rotation.player_preferences ALTER COLUMN hud_scale SET DEFAULT 80;
            ALTER TABLE map_rotation.player_preferences ADD CONSTRAINT ck_player_preferences_dock_side CHECK (dock_side IN ('left', 'right'));
            ALTER TABLE map_rotation.player_preferences ADD CONSTRAINT ck_player_preferences_animation CHECK (animation IN ('none', 'fast', 'normal', 'slow'));
            ALTER TABLE map_rotation.settings ALTER COLUMN menu_items_per_page SET DEFAULT 12;
            UPDATE map_rotation.settings
            SET vote_options_count = LEAST(vote_options_count, 6),
                nomination_slots = LEAST(nomination_slots, vote_options_count, 6),
                menu_items_per_page = 12,
                configuration_version = configuration_version + 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE map_rotation.player_preferences DROP CONSTRAINT ck_player_preferences_dock_side;
            ALTER TABLE map_rotation.player_preferences DROP CONSTRAINT ck_player_preferences_animation;
            ALTER TABLE map_rotation.player_preferences DROP COLUMN dock_side;
            ALTER TABLE map_rotation.player_preferences DROP COLUMN animation;
            ALTER TABLE map_rotation.player_preferences ALTER COLUMN hud_scale SET DEFAULT 100;
            ALTER TABLE map_rotation.settings ALTER COLUMN menu_items_per_page SET DEFAULT 5;
            UPDATE map_rotation.settings SET menu_items_per_page = LEAST(menu_items_per_page, 10);
            """);
        // Ключи карт сохраняются: CMS может продолжать использовать их после отката сервера.
    }
}
