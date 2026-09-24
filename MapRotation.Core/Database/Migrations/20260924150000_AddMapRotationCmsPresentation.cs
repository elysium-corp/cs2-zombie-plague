using Microsoft.EntityFrameworkCore.Migrations;

namespace MapRotation.Core.Database.Migrations;

/// <summary>Добавляет необязательные изображения и настройки редактора без изменения ротации.</summary>
public partial class AddMapRotationCmsPresentation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("hud_image_path", "maps", schema: "map_rotation",
            type: "character varying(256)", maxLength: 256, nullable: true);
        migrationBuilder.AddColumn<string>("hud_settings", "settings", schema: "map_rotation",
            type: "jsonb", nullable: false, defaultValue: "{}");
        migrationBuilder.AddColumn<int>("menu_items_per_page", "settings", schema: "map_rotation",
            type: "integer", nullable: false, defaultValue: 5);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("hud_image_path", "maps", "map_rotation");
        migrationBuilder.DropColumn("hud_settings", "settings", "map_rotation");
        migrationBuilder.DropColumn("menu_items_per_page", "settings", "map_rotation");
    }
}
