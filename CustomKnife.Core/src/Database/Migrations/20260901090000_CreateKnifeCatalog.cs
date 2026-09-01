using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomKnife.src.Database.Migrations;

/// <inheritdoc />
[DbContext(typeof(CustomKnifeDbContext))]
[Migration("20260901090000_CreateKnifeCatalog")]
public sealed class CreateKnifeCatalog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: CustomKnifeDbContext.SchemaName);

        migrationBuilder.CreateTable(
            name: "knives",
            schema: CustomKnifeDbContext.SchemaName,
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                internal_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                model = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                speed = table.Column<float>(type: "real", nullable: false),
                knockback_recoil = table.Column<float>(type: "real", nullable: false),
                knockback_pick_distance = table.Column<float>(type: "real", nullable: false),
                gravity = table.Column<int>(type: "integer", nullable: false),
                damage_multiplier = table.Column<float>(type: "real", nullable: false),
                required_permission = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_knives", x => x.id);
                table.CheckConstraint("CK_knives_damage_multiplier", "damage_multiplier >= 0 AND damage_multiplier <= 1000");
                table.CheckConstraint("CK_knives_gravity", "gravity >= 1 AND gravity <= 10000");
                table.CheckConstraint("CK_knives_knockback", "knockback_recoil >= 0 AND knockback_recoil <= 100000 AND knockback_pick_distance >= 0 AND knockback_pick_distance <= 100000");
                table.CheckConstraint("CK_knives_required_permission", "required_permission IS NULL OR required_permission ~ '^[a-z0-9_.:-]+$'");
                table.CheckConstraint("CK_knives_speed", "speed >= 1 AND speed <= 2000");
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_knives_internal_name",
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives",
            column: "internal_name",
            unique: true
        );

        var timestamp = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

        migrationBuilder.InsertData(
            schema: CustomKnifeDbContext.SchemaName,
            table: "knives",
            columns:
            [
                "internal_name", "display_name", "description", "model", "speed",
                "knockback_recoil", "knockback_pick_distance", "gravity", "damage_multiplier",
                "required_permission", "enabled", "sort_order", "created_at", "updated_at"
            ],
            values: new object[,]
            {
                {
                    "knife_piercer", "Piercer", "Отдача",
                    "weapons/nozb1/valogun/knife/sovereign_tactical/sovereign_tactical_ag2.vmdl",
                    250f, 1400f, 150f, 800, 1f, null, true, 10, timestamp, timestamp
                },
                {
                    "knife_spike", "Spike", "Скорость",
                    "weapons/nozb1/valogun/knife/ejderbicak_cord/ejderbicak_cord_ag2.vmdl",
                    300f, 250f, 150f, 800, 1f, null, true, 20, timestamp, timestamp
                },
                {
                    "knife_axe", "Axe", "Гравитация",
                    "weapons/nozb1/valogun/knife/ashen_kukri/ashen_kukri_ag2.vmdl",
                    250f, 250f, 150f, 600, 1f, null, true, 30, timestamp, timestamp
                },
                {
                    "knife_katana", "Katana", "VIP",
                    "weapons/nozb1/valogun/knife/oni_katana_tactical/oni_katana_tactical_ag2.vmdl",
                    300f, 1400f, 150f, 550, 3f, null, true, 40, timestamp, timestamp
                }
            }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "knives", schema: CustomKnifeDbContext.SchemaName);
    }
}
