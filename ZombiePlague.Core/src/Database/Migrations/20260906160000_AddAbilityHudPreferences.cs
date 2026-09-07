using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZombiePlague.Core.Database.Migrations;

[DbContext(typeof(ZombiePlagueDbContext))]
[Migration("20260906160000_AddAbilityHudPreferences")]
internal sealed partial class AddAbilityHudPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "ability_hud_scale", schema: "zombie_plague", table: "players",
            type: "integer", nullable: false, defaultValue: 100);
        migrationBuilder.AddColumn<string>(name: "ability_hud_position", schema: "zombie_plague", table: "players",
            type: "character varying(24)", maxLength: 24, nullable: false, defaultValue: "bottom_center");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ability_hud_scale", schema: "zombie_plague", table: "players");
        migrationBuilder.DropColumn(name: "ability_hud_position", schema: "zombie_plague", table: "players");
    }
}
