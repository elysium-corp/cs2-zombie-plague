using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomEquipment.Database.Migrations;

[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260905120000_AllowWeaponSoundVariants")]
internal sealed class AllowWeaponSoundVariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_weapon_sounds_weapon_id_trigger_event_name",
            schema: "custom_equipment",
            table: "weapon_sounds",
            columns: new[] { "weapon_id", "trigger", "event_name" },
            unique: true);

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX "IX_weapon_sounds_weapon_id_trigger_event_name_lower"
                ON custom_equipment.weapon_sounds (weapon_id, lower(trigger), lower(event_name));
            DROP INDEX custom_equipment."IX_weapon_sounds_event_name_lower";
            DROP INDEX custom_equipment."IX_weapon_sounds_weapon_id_trigger_lower";
            """);

        migrationBuilder.DropIndex(
            name: "IX_weapon_sounds_event_name",
            schema: "custom_equipment",
            table: "weapon_sounds");
        migrationBuilder.DropIndex(
            name: "IX_weapon_sounds_weapon_id_trigger",
            schema: "custom_equipment",
            table: "weapon_sounds");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Откат с несовместимыми вариантами отклоняется уникальными индексами, не удаляя звуки.
        migrationBuilder.CreateIndex(
            name: "IX_weapon_sounds_event_name",
            schema: "custom_equipment",
            table: "weapon_sounds",
            column: "event_name",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_weapon_sounds_weapon_id_trigger",
            schema: "custom_equipment",
            table: "weapon_sounds",
            columns: new[] { "weapon_id", "trigger" },
            unique: true);

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX "IX_weapon_sounds_event_name_lower"
                ON custom_equipment.weapon_sounds (lower(event_name));
            CREATE UNIQUE INDEX "IX_weapon_sounds_weapon_id_trigger_lower"
                ON custom_equipment.weapon_sounds (weapon_id, lower(trigger));
            DROP INDEX custom_equipment."IX_weapon_sounds_weapon_id_trigger_event_name_lower";
            """);

        migrationBuilder.DropIndex(
            name: "IX_weapon_sounds_weapon_id_trigger_event_name",
            schema: "custom_equipment",
            table: "weapon_sounds");
    }
}
