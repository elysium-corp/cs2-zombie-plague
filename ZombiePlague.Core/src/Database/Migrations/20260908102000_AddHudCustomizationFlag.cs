using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZombiePlague.Core.Database.Migrations;

[DbContext(typeof(ZombiePlagueDbContext))]
[Migration("20260908102000_AddHudCustomizationFlag")]
internal sealed class AddHudCustomizationFlag : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE zombie_plague.players ADD COLUMN ability_hud_customized boolean NOT NULL DEFAULT FALSE;
        UPDATE zombie_plague.players SET ability_hud_customized = TRUE;
        """);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "ability_hud_customized", schema: "zombie_plague", table: "players");
}
