using Microsoft.EntityFrameworkCore.Migrations;

namespace MapRotation.Core.Database.Migrations;

/// <summary>Хранит личный вид HUD отдельно от каталога и настроек ротации.</summary>
public partial class AddMapRotationHudPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "player_preferences", schema: "map_rotation",
        columns: table => new
        {
            steam_id = table.Column<long>(type: "bigint", nullable: false),
            orientation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "horizontal"),
            hud_scale = table.Column<int>(type: "integer", nullable: false, defaultValue: 100)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_player_preferences", x => x.steam_id);
            table.CheckConstraint("ck_player_preferences_orientation", "orientation IN ('horizontal', 'vertical')");
            table.CheckConstraint("ck_player_preferences_hud_scale", "hud_scale IN (80, 100, 120)");
        });

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("player_preferences", "map_rotation");
}
