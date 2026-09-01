using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomEquipment.Database.Migrations;

/// <inheritdoc />
[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260901143000_CreateEquipmentShops")]
public sealed class CreateEquipmentShops : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: CustomEquipmentDbContext.SchemaName);

        migrationBuilder.CreateTable(
            name: "shop_categories",
            schema: CustomEquipmentDbContext.SchemaName,
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation(
                        "Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                    ),
                shop_type = table.Column<string>(
                    type: "character varying(16)",
                    maxLength: 16,
                    nullable: false
                ),
                key = table.Column<string>(
                    type: "character varying(64)",
                    maxLength: 64,
                    nullable: false
                ),
                display_name = table.Column<string>(
                    type: "character varying(128)",
                    maxLength: 128,
                    nullable: false
                ),
                description = table.Column<string>(
                    type: "character varying(512)",
                    maxLength: 512,
                    nullable: false
                ),
                enabled = table.Column<bool>(
                    type: "boolean",
                    nullable: false,
                    defaultValue: true
                ),
                sort_order = table.Column<int>(
                    type: "integer",
                    nullable: false,
                    defaultValue: 0
                ),
                created_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"
                ),
                updated_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"
                )
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shop_categories", x => x.id);
                table.CheckConstraint(
                    "CK_shop_categories_type",
                    "shop_type IN ('human', 'zombie')"
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "shop_products",
            schema: CustomEquipmentDbContext.SchemaName,
            columns: table => new
            {
                implementation_key = table.Column<string>(
                    type: "character varying(64)",
                    maxLength: 64,
                    nullable: false
                ),
                internal_name = table.Column<string>(
                    type: "character varying(128)",
                    maxLength: 128,
                    nullable: false
                ),
                display_name = table.Column<string>(
                    type: "character varying(128)",
                    maxLength: 128,
                    nullable: false
                ),
                enabled = table.Column<bool>(
                    type: "boolean",
                    nullable: false,
                    defaultValue: true
                ),
                sort_order = table.Column<int>(
                    type: "integer",
                    nullable: false,
                    defaultValue: 0
                ),
                created_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"
                ),
                updated_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"
                )
            },
            constraints: table => table.PrimaryKey(
                "PK_shop_products",
                x => x.implementation_key
            )
        );

        migrationBuilder.CreateTable(
            name: "shop_role_limits",
            schema: CustomEquipmentDbContext.SchemaName,
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation(
                        "Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                    ),
                shop_type = table.Column<string>(
                    type: "character varying(16)",
                    maxLength: 16,
                    nullable: false
                ),
                privilege_key = table.Column<string>(
                    type: "character varying(129)",
                    maxLength: 129,
                    nullable: false
                ),
                max_purchases_per_round = table.Column<int>(
                    type: "integer",
                    nullable: false
                ),
                max_purchases_per_map = table.Column<int>(
                    type: "integer",
                    nullable: false
                ),
                enabled = table.Column<bool>(
                    type: "boolean",
                    nullable: false,
                    defaultValue: true
                ),
                sort_order = table.Column<int>(
                    type: "integer",
                    nullable: false,
                    defaultValue: 0
                ),
                created_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"
                ),
                updated_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"
                )
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shop_role_limits", x => x.id);
                table.CheckConstraint(
                    "CK_shop_role_limits_type",
                    "shop_type IN ('human', 'zombie')"
                );
                table.CheckConstraint(
                    "CK_shop_role_limits_values",
                    "max_purchases_per_round >= 0 AND max_purchases_per_map >= 0"
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "shop_settings",
            schema: CustomEquipmentDbContext.SchemaName,
            columns: table => new
            {
                shop_type = table.Column<string>(
                    type: "character varying(16)",
                    maxLength: 16,
                    nullable: false
                ),
                display_name = table.Column<string>(
                    type: "character varying(128)",
                    maxLength: 128,
                    nullable: false
                ),
                enabled = table.Column<bool>(
                    type: "boolean",
                    nullable: false,
                    defaultValue: true
                ),
                max_purchases_per_round = table.Column<int>(
                    type: "integer",
                    nullable: false,
                    defaultValue: 0
                ),
                max_purchases_per_map = table.Column<int>(
                    type: "integer",
                    nullable: false,
                    defaultValue: 0
                ),
                created_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"
                ),
                updated_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"
                )
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shop_settings", x => x.shop_type);
                table.CheckConstraint(
                    "CK_shop_settings_type",
                    "shop_type IN ('human', 'zombie')"
                );
                table.CheckConstraint(
                    "CK_shop_settings_limits",
                    "max_purchases_per_round >= 0 AND max_purchases_per_map >= 0"
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "shop_listings",
            schema: CustomEquipmentDbContext.SchemaName,
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation(
                        "Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                    ),
                shop_type = table.Column<string>(
                    type: "character varying(16)",
                    maxLength: 16,
                    nullable: false
                ),
                item_internal_name = table.Column<string>(
                    type: "character varying(128)",
                    maxLength: 128,
                    nullable: false
                ),
                category_id = table.Column<long>(type: "bigint", nullable: false),
                description = table.Column<string>(
                    type: "character varying(1024)",
                    maxLength: 1024,
                    nullable: false,
                    defaultValue: ""
                ),
                price = table.Column<int>(type: "integer", nullable: false),
                max_purchases_per_round = table.Column<int>(
                    type: "integer",
                    nullable: false,
                    defaultValue: 0
                ),
                max_purchases_per_map = table.Column<int>(
                    type: "integer",
                    nullable: false,
                    defaultValue: 0
                ),
                enabled = table.Column<bool>(
                    type: "boolean",
                    nullable: false,
                    defaultValue: true
                ),
                sort_order = table.Column<int>(
                    type: "integer",
                    nullable: false,
                    defaultValue: 0
                ),
                settings = table.Column<string>(
                    type: "jsonb",
                    nullable: false,
                    defaultValueSql: "'{}'::jsonb"
                ),
                created_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"
                ),
                updated_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "CURRENT_TIMESTAMP"
                )
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shop_listings", x => x.id);
                table.CheckConstraint(
                    "CK_shop_listings_type",
                    "shop_type IN ('human', 'zombie')"
                );
                table.CheckConstraint(
                    "CK_shop_listings_limits",
                    "price >= 0 AND max_purchases_per_round >= 0 AND max_purchases_per_map >= 0"
                );
                table.CheckConstraint(
                    "CK_shop_listings_settings_object",
                    "jsonb_typeof(settings) = 'object'"
                );
                table.ForeignKey(
                    name: "FK_shop_listings_shop_categories_category_id",
                    column: x => x.category_id,
                    principalSchema: CustomEquipmentDbContext.SchemaName,
                    principalTable: "shop_categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_shop_categories_shop_type_enabled_sort_order",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_categories",
            columns: ["shop_type", "enabled", "sort_order"]
        );
        migrationBuilder.CreateIndex(
            name: "IX_shop_categories_shop_type_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_categories",
            columns: ["shop_type", "key"],
            unique: true
        );
        migrationBuilder.CreateIndex(
            name: "IX_shop_listings_category_id",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_listings",
            column: "category_id"
        );
        migrationBuilder.CreateIndex(
            name: "IX_shop_listings_shop_type_enabled_sort_order",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_listings",
            columns: ["shop_type", "enabled", "sort_order"]
        );
        migrationBuilder.CreateIndex(
            name: "IX_shop_listings_shop_type_item_internal_name",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_listings",
            columns: ["shop_type", "item_internal_name"],
            unique: true
        );
        migrationBuilder.CreateIndex(
            name: "IX_shop_products_internal_name",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_products",
            column: "internal_name",
            unique: true
        );
        migrationBuilder.CreateIndex(
            name: "IX_shop_role_limits_shop_type_enabled_sort_order",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_role_limits",
            columns: ["shop_type", "enabled", "sort_order"]
        );
        migrationBuilder.CreateIndex(
            name: "IX_shop_role_limits_shop_type_privilege_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_role_limits",
            columns: ["shop_type", "privilege_key"],
            unique: true
        );

        migrationBuilder.Sql(
            """
            INSERT INTO custom_equipment.shop_settings
                (shop_type, display_name, enabled, max_purchases_per_round, max_purchases_per_map)
            VALUES
                ('human', 'Магазин людей', TRUE, 0, 0),
                ('zombie', 'Магазин зомби', TRUE, 0, 0);

            INSERT INTO custom_equipment.shop_categories
                (id, shop_type, key, display_name, description, enabled, sort_order)
            VALUES
                (1,  'human', 'pistol',          'Пистолеты',             '', TRUE, 0),
                (2,  'human', 'submachine_gun', 'Пистолеты-пулемёты',    '', TRUE, 10),
                (3,  'human', 'rifle',           'Винтовки',              '', TRUE, 20),
                (4,  'human', 'shotgun',         'Дробовики',             '', TRUE, 30),
                (5,  'human', 'sniper_rifle',    'Снайперские винтовки',  '', TRUE, 40),
                (6,  'human', 'machine_gun',     'Пулемёты',              '', TRUE, 50),
                (7,  'human', 'grenade',         'Гранаты',               '', TRUE, 60),
                (8,  'human', 'equipment',       'Экипировка',            '', TRUE, 70),
                (9,  'zombie', 'pistol',          'Пистолеты',             '', TRUE, 0),
                (10, 'zombie', 'submachine_gun', 'Пистолеты-пулемёты',    '', TRUE, 10),
                (11, 'zombie', 'rifle',           'Винтовки',              '', TRUE, 20),
                (12, 'zombie', 'shotgun',         'Дробовики',             '', TRUE, 30),
                (13, 'zombie', 'sniper_rifle',    'Снайперские винтовки',  '', TRUE, 40),
                (14, 'zombie', 'machine_gun',     'Пулемёты',              '', TRUE, 50),
                (15, 'zombie', 'grenade',         'Гранаты',               '', TRUE, 60),
                (16, 'zombie', 'equipment',       'Экипировка',            '', TRUE, 70);

            SELECT setval(
                pg_get_serial_sequence('custom_equipment.shop_categories', 'id'),
                16,
                TRUE
            );

            INSERT INTO custom_equipment.shop_products
                (implementation_key, internal_name, display_name, enabled, sort_order)
            VALUES
                ('armor', 'custom_equipment:armor', 'Броня', TRUE, 1000);

            INSERT INTO custom_equipment.shop_listings
                (
                    shop_type, item_internal_name, category_id, description, price,
                    max_purchases_per_round, max_purchases_per_map, enabled, sort_order,
                    settings
                )
            SELECT
                shop.shop_type,
                weapon.internal_name,
                category.id,
                '',
                weapon.item_price,
                0,
                0,
                TRUE,
                weapon.sort_order,
                '{}'::jsonb
            FROM custom_equipment.weapons AS weapon
            CROSS JOIN (
                VALUES ('human', 1), ('zombie', 2)
            ) AS shop(shop_type, access_flag)
            JOIN custom_equipment.shop_categories AS category
                ON category.shop_type = shop.shop_type
                AND category.key = CASE lower(weapon.weapon_type)
                    WHEN 'submachinegun' THEN 'submachine_gun'
                    WHEN 'sniperrifle' THEN 'sniper_rifle'
                    WHEN 'machinegun' THEN 'machine_gun'
                    ELSE lower(weapon.weapon_type)
                END
            WHERE (weapon.access_flags & shop.access_flag) <> 0;

            INSERT INTO custom_equipment.shop_listings
                (
                    shop_type, item_internal_name, category_id, description, price,
                    max_purchases_per_round, max_purchases_per_map, enabled, sort_order,
                    settings
                )
            SELECT
                shop.shop_type,
                item.internal_name,
                category.id,
                '',
                item.item_price,
                0,
                0,
                item.enabled,
                item.sort_order,
                '{}'::jsonb
            FROM custom_equipment.gameplay_items AS item
            CROSS JOIN (
                VALUES ('human', 1), ('zombie', 2)
            ) AS shop(shop_type, access_flag)
            JOIN custom_equipment.shop_categories AS category
                ON category.shop_type = shop.shop_type
                AND category.key = CASE
                    WHEN item.implementation_key = 'laser_mine' THEN 'equipment'
                    ELSE 'grenade'
                END
            WHERE (item.access_flags & shop.access_flag) <> 0;

            INSERT INTO custom_equipment.shop_listings
                (
                    shop_type, item_internal_name, category_id, description, price,
                    max_purchases_per_round, max_purchases_per_map, enabled, sort_order,
                    settings
                )
            SELECT
                'human',
                'custom_equipment:armor',
                category.id,
                'Добавляет указанное количество брони, но не выше 100',
                100,
                1,
                0,
                TRUE,
                1000,
                '{"armor_amount":50}'::jsonb
            FROM custom_equipment.shop_categories AS category
            WHERE category.shop_type = 'human' AND category.key = 'equipment';
            """
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "shop_listings",
            schema: CustomEquipmentDbContext.SchemaName
        );
        migrationBuilder.DropTable(
            name: "shop_products",
            schema: CustomEquipmentDbContext.SchemaName
        );
        migrationBuilder.DropTable(
            name: "shop_role_limits",
            schema: CustomEquipmentDbContext.SchemaName
        );
        migrationBuilder.DropTable(
            name: "shop_settings",
            schema: CustomEquipmentDbContext.SchemaName
        );
        migrationBuilder.DropTable(
            name: "shop_categories",
            schema: CustomEquipmentDbContext.SchemaName
        );
    }
}
