using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomEquipment.Database.Migrations;

[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260824120000_CreateCustomEquipmentCatalog")]
public sealed class CreateCustomEquipmentCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: CustomEquipmentDbContext.SchemaName);

        migrationBuilder.CreateTable(
            name: "weapons",
            schema: CustomEquipmentDbContext.SchemaName,
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                internal_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                inheritor_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                subclass_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                weapon_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                access_flags = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                rarity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Common"),
                model = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                item_price = table.Column<int>(type: "integer", nullable: false),
                ammo_price = table.Column<int>(type: "integer", nullable: true),
                clip_size = table.Column<int>(type: "integer", nullable: true),
                reserve_ammo = table.Column<int>(type: "integer", nullable: true),
                cycle_time_primary = table.Column<float>(type: "real", nullable: true),
                cycle_time_secondary = table.Column<float>(type: "real", nullable: true),
                deploy_duration = table.Column<float>(type: "real", nullable: true),
                num_bullets = table.Column<int>(type: "integer", nullable: true),
                penetration = table.Column<float>(type: "real", nullable: true),
                effective_range = table.Column<float>(type: "real", nullable: true),
                range_modifier = table.Column<float>(type: "real", nullable: true),
                damage_head = table.Column<float>(type: "real", nullable: true),
                damage_chest = table.Column<float>(type: "real", nullable: true),
                damage_stomach = table.Column<float>(type: "real", nullable: true),
                damage_left_arm = table.Column<float>(type: "real", nullable: true),
                damage_right_arm = table.Column<float>(type: "real", nullable: true),
                damage_left_leg = table.Column<float>(type: "real", nullable: true),
                damage_right_leg = table.Column<float>(type: "real", nullable: true),
                damage_neck = table.Column<float>(type: "real", nullable: true),
                particle_tracer = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                particle_impact = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                particle_muzzle_flash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_weapons", x => x.id);
                table.CheckConstraint("CK_weapons_access_flags", "access_flags >= 0 AND access_flags <= 3");
                table.CheckConstraint("CK_weapons_ammo_price", "ammo_price IS NULL OR ammo_price >= 0");
                table.CheckConstraint("CK_weapons_ammunition", "(clip_size IS NULL OR clip_size >= 0) AND (reserve_ammo IS NULL OR reserve_ammo >= 0)");
                table.CheckConstraint("CK_weapons_ballistics", "(num_bullets IS NULL OR num_bullets >= 1) AND (penetration IS NULL OR penetration >= 0) AND (effective_range IS NULL OR effective_range >= 0) AND (range_modifier IS NULL OR range_modifier >= 0)");
                table.CheckConstraint("CK_weapons_damage", "(damage_head IS NULL OR damage_head >= 0) AND (damage_chest IS NULL OR damage_chest >= 0) AND (damage_stomach IS NULL OR damage_stomach >= 0) AND (damage_left_arm IS NULL OR damage_left_arm >= 0) AND (damage_right_arm IS NULL OR damage_right_arm >= 0) AND (damage_left_leg IS NULL OR damage_left_leg >= 0) AND (damage_right_leg IS NULL OR damage_right_leg >= 0) AND (damage_neck IS NULL OR damage_neck >= 0)");
                table.CheckConstraint("CK_weapons_item_price", "item_price >= 0");
                table.CheckConstraint("CK_weapons_timing", "(cycle_time_primary IS NULL OR cycle_time_primary > 0) AND (cycle_time_secondary IS NULL OR (cycle_time_secondary > 0 AND cycle_time_primary IS NOT NULL)) AND (deploy_duration IS NULL OR deploy_duration >= 0)");
            }
        );

        migrationBuilder.CreateTable(
            name: "weapon_sounds",
            schema: CustomEquipmentDbContext.SchemaName,
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                weapon_id = table.Column<long>(type: "bigint", nullable: false),
                trigger = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                event_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                replaces_event_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                sound_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "csgo_mega"),
                volume = table.Column<float>(type: "real", nullable: false, defaultValue: 1.0f),
                pitch = table.Column<float>(type: "real", nullable: false, defaultValue: 1.0f),
                mix_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Weapons"),
                preload_vsnds = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                extra_properties = table.Column<string>(type: "jsonb", nullable: true),
                enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_weapon_sounds", x => x.id);
                table.CheckConstraint("CK_weapon_sounds_pitch", "pitch > 0");
                table.CheckConstraint("CK_weapon_sounds_volume", "volume >= 0");
                table.ForeignKey(
                    name: "FK_weapon_sounds_weapons_weapon_id",
                    column: x => x.weapon_id,
                    principalSchema: CustomEquipmentDbContext.SchemaName,
                    principalTable: "weapons",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "weapon_sound_files",
            schema: CustomEquipmentDbContext.SchemaName,
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                sound_id = table.Column<long>(type: "bigint", nullable: false),
                track = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                file_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_weapon_sound_files", x => x.id);
                table.CheckConstraint("CK_weapon_sound_files_track", "track >= 1 AND track <= 99");
                table.ForeignKey(
                    name: "FK_weapon_sound_files_weapon_sounds_sound_id",
                    column: x => x.sound_id,
                    principalSchema: CustomEquipmentDbContext.SchemaName,
                    principalTable: "weapon_sounds",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_weapons_enabled_sort_order",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "weapons",
            columns: new[] { "enabled", "sort_order" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_weapons_internal_name",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "weapons",
            column: "internal_name",
            unique: true
        );

        migrationBuilder.Sql(
            "CREATE UNIQUE INDEX \"IX_weapons_internal_name_lower\" ON custom_equipment.weapons (lower(internal_name));"
        );

        migrationBuilder.CreateIndex(
            name: "IX_weapon_sounds_event_name",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "weapon_sounds",
            column: "event_name",
            unique: true
        );

        migrationBuilder.Sql(
            "CREATE UNIQUE INDEX \"IX_weapon_sounds_event_name_lower\" ON custom_equipment.weapon_sounds (lower(event_name));"
        );

        migrationBuilder.CreateIndex(
            name: "IX_weapon_sounds_weapon_id_trigger",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "weapon_sounds",
            columns: new[] { "weapon_id", "trigger" },
            unique: true
        );

        migrationBuilder.Sql(
            "CREATE UNIQUE INDEX \"IX_weapon_sounds_weapon_id_trigger_lower\" ON custom_equipment.weapon_sounds (weapon_id, lower(trigger));"
        );

        migrationBuilder.CreateIndex(
            name: "IX_weapon_sound_files_sound_id_track_sort_order",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "weapon_sound_files",
            columns: new[] { "sound_id", "track", "sort_order" }
        );

        SeedExistingWeapons(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "weapon_sound_files", schema: CustomEquipmentDbContext.SchemaName);
        migrationBuilder.DropTable(name: "weapon_sounds", schema: CustomEquipmentDbContext.SchemaName);
        migrationBuilder.DropTable(name: "weapons", schema: CustomEquipmentDbContext.SchemaName);
    }

    private static void SeedExistingWeapons(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO custom_equipment.weapons
            (
                internal_name, display_name, inheritor_name, subclass_name, slot, weapon_type,
                access_flags, rarity, model, item_price, ammo_price, clip_size, reserve_ammo,
                cycle_time_primary, cycle_time_secondary, deploy_duration, num_bullets, penetration,
                effective_range, range_modifier, damage_head, damage_chest, damage_stomach,
                damage_left_arm, damage_right_arm, damage_left_leg, damage_right_leg, damage_neck,
                particle_tracer, particle_impact, particle_muzzle_flash, enabled, sort_order
            )
            VALUES
            ('custom_equipment:ajm', 'CZ75 Ajm', 'cz75a', 'weapon_ajm9_cz75', 'Secondary', 'Pistol',
             1, 'Uncommon', 'weapons/luci/ajm9_cz75/ajm9_cz75.vmdl', 7500, 375, 20, 5,
             0.07, 0.08, NULL, 1, 1, 10000, 1, 1.45, 1.85, 1.75, 1.55, 1.55, 1.65, 1.65, 1,
             NULL, NULL, NULL, TRUE, 10),
            ('custom_equipment:blackline', 'MP9 Blackline', 'mp9', 'weapon_blackline', 'Primary', 'SubmachineGun',
             1, 'Uncommon', 'weapons/luci/psd_mp9/psd_mp9_ag2.vmdl', 10500, 350, 20, 8,
             0.1, 0.15, NULL, 1, 1, 10000, 1, 1.95, 2.75, 2.35, 2.15, 2.15, 2.35, 2.35, 1,
             NULL, NULL, NULL, TRUE, 20),
            ('custom_equipment:elite', 'SSG Elite', 'ssg08', 'weapon_elite_v2', 'Primary', 'Rifle',
             1, 'Uncommon', 'weapons/luci/parab_ssg/parab_ssg_ag2.vmdl', 15000, 750, 3, 5,
             1.455, 1.455, NULL, 1, 1, 10000, 1, 2.55, 3.55, 3.55, 2.25, 2.25, 2.45, 2.45, 1,
             'particles/kolka/shoteffects/tracer11.vpcf', NULL, NULL, TRUE, 30),
            ('custom_equipment:frostbyte', 'MP7 Frostbyte', 'mp7', 'weapon_frostbyte', 'Primary', 'SubmachineGun',
             1, 'Uncommon', 'weapons/luci/eov_mp5/eov_mp5_ag2.vmdl', 13500, 450, 10, 5,
             0.2, 0.6, NULL, 1, 1, 10000, 1, 2.95, 3.10, 3.10, 3.45, 3.45, 3.85, 3.85, 1,
             'particles/kolka/shoteffects/tracer1.vpcf', NULL, NULL, TRUE, 40),
            ('custom_equipment:lava', 'AK47 Lava', 'ak47', 'weapon_ak_117_lava', 'Primary', 'SubmachineGun',
             1, 'Uncommon', 'weapons/luci/ak_117_lava/ak_117_lava.vmdl', 19500, 650, 25, 10,
             0.12, 0.13, NULL, 1, 1, 10000, 1, 2.45, 2.85, 2.85, 2.65, 2.65, 3.15, 3.15, 1,
             'particles/kolka/shoteffects/tracer7.vpcf', NULL, NULL, TRUE, 50),
            ('custom_equipment:omega', 'Omega Shotgun', 'xm1014', 'weapon_omega', 'Primary', 'Shotgun',
             1, 'Uncommon', 'weapons/nozb1/valogun/araxys_bundle/araxys_sawedoff/araxys_sawedoff_ag2.vmdl', 14000, 700, 2, 6,
             0.8, 1.0, NULL, 1, 1, 10000, 1, 1.65, 1.85, 1.85, 2.05, 2.05, 2.15, 2.15, 1,
             NULL, NULL, NULL, TRUE, 60),
            ('custom_equipment:reactorleak', 'UMP45 ReactorLeak', 'ump45', 'weapon_reactorleak', 'Primary', 'SubmachineGun',
             1, 'Uncommon', 'weapons/luci/car_ump45/car_ump45_ag2.vmdl', 9500, 315, 20, 5,
             0.13, 0.15, NULL, 1, 1, 10000, 1, 1.65, 2.50, 2.50, 2.45, 2.45, 2.55, 2.55, 1,
             NULL, NULL, NULL, TRUE, 70),
            ('custom_equipment:reaver', 'Deagle Reaver', 'deagle', 'weapon_reaver_deagle', 'Secondary', 'Pistol',
             1, 'Uncommon', 'weapons/luci/reaver_deagle/reaver_deagle.vmdl', 9500, 450, 1, 5,
             1.5, 1.6, NULL, 1, 1, 10000, 1, 11.55, 8.45, 8.45, 9.45, 9.45, 10.45, 10.45, 1,
             'particles/kolka/shoteffects/tracer11.vpcf', NULL, NULL, TRUE, 80),
            ('custom_equipment:x3', 'M4A1-S X3', 'm4a1_silencer', 'weapon_x3', 'Primary', 'Rifle',
             1, 'Uncommon', 'weapons/luci/x3_m4a1/x3_m4a1_ag2.vmdl', 16500, 550, 25, 7,
             NULL, NULL, NULL, 1, 1, 10000, 1, 2.05, 2.85, 2.6, 2.45, 2.45, 2.85, 2.85, 1,
             NULL, NULL, NULL, TRUE, 90);
            """
        );
    }
}
