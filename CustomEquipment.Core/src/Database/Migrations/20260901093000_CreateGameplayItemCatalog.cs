using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomEquipment.Database.Migrations;

/// <inheritdoc />
[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260901093000_CreateGameplayItemCatalog")]
public sealed class CreateGameplayItemCatalog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: CustomEquipmentDbContext.SchemaName);

        migrationBuilder.CreateTable(
            name: "gameplay_items",
            schema: CustomEquipmentDbContext.SchemaName,
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                implementation_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                internal_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                inheritor_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                access_flags = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                rarity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Common"),
                model = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                item_price = table.Column<int>(type: "integer", nullable: false),
                enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                settings = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gameplay_items", x => x.id);
                table.CheckConstraint("CK_gameplay_items_access_flags", "access_flags >= 0 AND access_flags <= 3");
                table.CheckConstraint("CK_gameplay_items_item_price", "item_price >= 0");
                table.CheckConstraint("CK_gameplay_items_settings_object", "jsonb_typeof(settings) = 'object'");
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_gameplay_items_enabled_sort_order",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "gameplay_items",
            columns: ["enabled", "sort_order"]
        );

        migrationBuilder.CreateIndex(
            name: "IX_gameplay_items_implementation_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "gameplay_items",
            column: "implementation_key",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_gameplay_items_internal_name",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "gameplay_items",
            column: "internal_name",
            unique: true
        );

        var timestamp = new DateTime(2026, 9, 1, 9, 30, 0, DateTimeKind.Utc);

        migrationBuilder.InsertData(
            schema: CustomEquipmentDbContext.SchemaName,
            table: "gameplay_items",
            columns:
            [
                "implementation_key", "internal_name", "display_name", "inheritor_name",
                "access_flags", "rarity", "model", "item_price", "enabled", "sort_order",
                "settings", "created_at", "updated_at"
            ],
            values: new object[,]
            {
                {
                    "barrier_nade", "custom_equipment:barrier_nade", "Barrier Nade", "smokegrenade",
                    (short)1, "Rare", "weapons/luci/elysium_smoke/elysium_smoke_ag2.vmdl", 100, true, 10,
                    """{"particle":"particles/barrier_nade.vpcf","knock_sound":"ZombiePlague.barrier_impact","environment_sound":"ZombiePlague.barrier_environment","environment_volume":0.65,"radius":175,"duration":15,"tick_interval":0.05,"horizontal_knockback":200,"ground_z_boost":150,"air_z_boost":25}""",
                    timestamp, timestamp
                },
                {
                    "fire_nade", "custom_equipment:fire_nade", "Fire Nade", "incgrenade",
                    (short)1, "Uncommon", "weapons/luci/incenderiary_gren/incenderiary_gren_ag2.vmdl", 100, true, 20,
                    """{"radius":275,"duration":8,"damage_per_tick_percent":2,"instant_damage_percent":5}""",
                    timestamp, timestamp
                },
                {
                    "frost_nade", "custom_equipment:frost_nade", "Frost Nade", "hegrenade",
                    (short)1, "Rare", "weapons/luci/sifi_hegrenade/sifi_hegrenade_ag2.vmdl", 100, true, 30,
                    """{"radius":250,"duration":5,"damage_reduction":0.1}""",
                    timestamp, timestamp
                },
                {
                    "jump_nade", "custom_equipment:jump_nade", "Jump Nade", "hegrenade",
                    (short)2, "Uncommon", "models/throwhead/throwhead2_ag2.vmdl", 100, true, 40,
                    """{"radius":250,"power":1050}""",
                    timestamp, timestamp
                },
                {
                    "shake_nade", "custom_equipment:shake_nade", "Shake Nade", "smokegrenade",
                    (short)2, "Rare", "models/throwhead/throwhead_ag2.vmdl", 100, true, 50,
                    """{"radius":250,"duration":10}""",
                    timestamp, timestamp
                },
                {
                    "laser_mine", "custom_equipment:laser_mine", "Laser Mine", "c4",
                    (short)1, "Rare", "models/lasermine.vmdl", 100, true, 60,
                    """{"mine_model":"models/lasermine.vmdl","trigger_interval":0.15,"damage_per_trigger":35,"tracer_distance":2000,"max_health":100,"beam_width":0.5,"beam_red":0,"beam_green":0,"beam_blue":255,"beam_alpha":255,"max_distance_to_attach":100,"setup_duration":1,"update_interval_ms":100}""",
                    timestamp, timestamp
                }
            }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gameplay_items", schema: CustomEquipmentDbContext.SchemaName);
    }
}
