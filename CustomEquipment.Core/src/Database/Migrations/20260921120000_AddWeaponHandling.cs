using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CustomEquipment.Database.Migrations;

[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260921120000_AddWeaponHandling")]
internal sealed class AddWeaponHandling : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "recoil", schema: "custom_equipment", table: "weapons", type: "jsonb", nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "accuracy", schema: "custom_equipment", table: "weapons", type: "jsonb", nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_weapons_recoil_object", schema: "custom_equipment", table: "weapons",
            sql: "recoil IS NULL OR jsonb_typeof(recoil) = 'object'");
        migrationBuilder.AddCheckConstraint(
            name: "CK_weapons_accuracy_object", schema: "custom_equipment", table: "weapons",
            sql: "accuracy IS NULL OR jsonb_typeof(accuracy) = 'object'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_weapons_recoil_object", schema: "custom_equipment", table: "weapons");
        migrationBuilder.DropCheckConstraint(
            name: "CK_weapons_accuracy_object", schema: "custom_equipment", table: "weapons");
        migrationBuilder.DropColumn(name: "recoil", schema: "custom_equipment", table: "weapons");
        migrationBuilder.DropColumn(name: "accuracy", schema: "custom_equipment", table: "weapons");
    }
}
