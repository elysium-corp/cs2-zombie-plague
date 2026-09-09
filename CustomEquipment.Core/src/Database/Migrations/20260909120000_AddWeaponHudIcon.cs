using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CustomEquipment.Database.Migrations;

[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260909120000_AddWeaponHudIcon")]
internal sealed class AddWeaponHudIcon : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: "hud_icon_path", schema: "custom_equipment", table: "weapons", type: "character varying(512)",
        maxLength: 512, nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "hud_icon_path", schema: "custom_equipment", table: "weapons");
}
