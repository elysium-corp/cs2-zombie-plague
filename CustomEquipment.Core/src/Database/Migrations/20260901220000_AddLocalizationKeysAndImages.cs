using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomEquipment.Database.Migrations;

[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260901220000_AddLocalizationKeysAndImages")]
public sealed class AddLocalizationKeysAndImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddDisplayNameKey(migrationBuilder, "weapons");
        AddDisplayNameKey(migrationBuilder, "gameplay_items");
        AddDisplayNameKey(migrationBuilder, "shop_settings");
        AddDisplayNameKey(migrationBuilder, "shop_categories");
        AddDisplayNameKey(migrationBuilder, "shop_products");

        migrationBuilder.AddColumn<string>(
            name: "description_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_categories",
            type: "character varying(191)",
            maxLength: 191,
            nullable: true
        );
        migrationBuilder.AddColumn<string>(
            name: "description_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_listings",
            type: "character varying(191)",
            maxLength: 191,
            nullable: true
        );
        AddImageUrl(migrationBuilder, "weapons");
        AddImageUrl(migrationBuilder, "gameplay_items");

        migrationBuilder.Sql(
            """
            UPDATE custom_equipment.weapons
            SET display_name_key =
                'Equipment.Item.' ||
                regexp_replace(replace(internal_name, ':', '.'), '[^A-Za-z0-9_.-]', '_', 'g') ||
                '.Name';

            UPDATE custom_equipment.gameplay_items
            SET display_name_key =
                'Equipment.Item.' ||
                regexp_replace(replace(internal_name, ':', '.'), '[^A-Za-z0-9_.-]', '_', 'g') ||
                '.Name';

            UPDATE custom_equipment.shop_settings
            SET display_name_key =
                'Equipment.Shop.' ||
                CASE shop_type WHEN 'human' THEN 'Human' ELSE 'Zombie' END ||
                '.Title';

            UPDATE custom_equipment.shop_categories
            SET display_name_key = CASE key
                WHEN 'pistol' THEN 'Menu.Equipment.Category.Pistol'
                WHEN 'submachine_gun' THEN 'Menu.Equipment.Category.SubmachineGun'
                WHEN 'rifle' THEN 'Menu.Equipment.Category.Rifle'
                WHEN 'shotgun' THEN 'Menu.Equipment.Category.Shotgun'
                WHEN 'sniper_rifle' THEN 'Menu.Equipment.Category.SniperRifle'
                WHEN 'machine_gun' THEN 'Menu.Equipment.Category.MachineGun'
                WHEN 'grenade' THEN 'Menu.Equipment.Category.Grenade'
                WHEN 'equipment' THEN 'Menu.Equipment.Category.Equipment'
                ELSE
                    'Equipment.Shop.' ||
                    CASE shop_type WHEN 'human' THEN 'Human' ELSE 'Zombie' END ||
                    '.Category.' ||
                    regexp_replace(key, '[^A-Za-z0-9_.-]', '_', 'g') ||
                    '.Name'
                END,
                description_key = CASE
                    WHEN btrim(description) = '' THEN NULL
                    ELSE
                        'Equipment.Shop.' ||
                        CASE shop_type WHEN 'human' THEN 'Human' ELSE 'Zombie' END ||
                        '.Category.' ||
                        regexp_replace(key, '[^A-Za-z0-9_.-]', '_', 'g') ||
                        '.Description'
                END;

            UPDATE custom_equipment.shop_listings
            SET description_key = CASE
                WHEN btrim(description) = '' THEN NULL
                ELSE
                    'Equipment.Shop.' ||
                    CASE shop_type WHEN 'human' THEN 'Human' ELSE 'Zombie' END ||
                    '.Item.' ||
                    regexp_replace(replace(item_internal_name, ':', '.'), '[^A-Za-z0-9_.-]', '_', 'g') ||
                    '.Description'
                END;

            UPDATE custom_equipment.shop_products
            SET display_name_key =
                'Equipment.Item.' ||
                regexp_replace(replace(internal_name, ':', '.'), '[^A-Za-z0-9_.-]', '_', 'g') ||
                '.Name';
            """
        );

        MakeDisplayNameKeyRequired(migrationBuilder, "weapons");
        MakeDisplayNameKeyRequired(migrationBuilder, "gameplay_items");
        MakeDisplayNameKeyRequired(migrationBuilder, "shop_settings");
        MakeDisplayNameKeyRequired(migrationBuilder, "shop_categories");
        MakeDisplayNameKeyRequired(migrationBuilder, "shop_products");

        migrationBuilder.AddCheckConstraint(
            name: "CK_weapons_display_name_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "weapons",
            sql: "display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$'"
        );
        migrationBuilder.AddCheckConstraint(
            name: "CK_weapons_image_url",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "weapons",
            sql: ImageUrlConstraint
        );
        migrationBuilder.AddCheckConstraint(
            name: "CK_gameplay_items_display_name_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "gameplay_items",
            sql: "display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$'"
        );
        migrationBuilder.AddCheckConstraint(
            name: "CK_gameplay_items_image_url",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "gameplay_items",
            sql: ImageUrlConstraint
        );
        migrationBuilder.AddCheckConstraint(
            name: "CK_shop_settings_display_name_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_settings",
            sql: "display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$'"
        );
        migrationBuilder.AddCheckConstraint(
            name: "CK_shop_categories_localization_keys",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_categories",
            sql: "display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$' AND (description_key IS NULL OR description_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$')"
        );
        migrationBuilder.AddCheckConstraint(
            name: "CK_shop_listings_description_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_listings",
            sql: "description_key IS NULL OR description_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$'"
        );
        migrationBuilder.AddCheckConstraint(
            name: "CK_shop_products_display_name_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: "shop_products",
            sql: "display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$'"
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropConstraint(migrationBuilder, "weapons", "CK_weapons_display_name_key");
        DropConstraint(migrationBuilder, "weapons", "CK_weapons_image_url");
        DropConstraint(migrationBuilder, "gameplay_items", "CK_gameplay_items_display_name_key");
        DropConstraint(migrationBuilder, "gameplay_items", "CK_gameplay_items_image_url");
        DropConstraint(migrationBuilder, "shop_settings", "CK_shop_settings_display_name_key");
        DropConstraint(migrationBuilder, "shop_categories", "CK_shop_categories_localization_keys");
        DropConstraint(migrationBuilder, "shop_listings", "CK_shop_listings_description_key");
        DropConstraint(migrationBuilder, "shop_products", "CK_shop_products_display_name_key");

        DropColumn(migrationBuilder, "weapons", "image_url");
        DropColumn(migrationBuilder, "gameplay_items", "image_url");
        DropColumn(migrationBuilder, "shop_categories", "description_key");
        DropColumn(migrationBuilder, "shop_listings", "description_key");
        DropColumn(migrationBuilder, "weapons", "display_name_key");
        DropColumn(migrationBuilder, "gameplay_items", "display_name_key");
        DropColumn(migrationBuilder, "shop_settings", "display_name_key");
        DropColumn(migrationBuilder, "shop_categories", "display_name_key");
        DropColumn(migrationBuilder, "shop_products", "display_name_key");
    }

    private const string ImageUrlConstraint =
        "image_url IS NULL OR image_url ~ '^https://[^[:space:]]+$' " +
        "OR image_url ~ '^assets/uploads/elysium-equipments/items/[a-f0-9]{40}\\.(jpg|jpeg|png|webp|avif)$'";

    private static void AddDisplayNameKey(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.AddColumn<string>(
            name: "display_name_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: table,
            type: "character varying(191)",
            maxLength: 191,
            nullable: true
        );
    }

    private static void MakeDisplayNameKeyRequired(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.AlterColumn<string>(
            name: "display_name_key",
            schema: CustomEquipmentDbContext.SchemaName,
            table: table,
            type: "character varying(191)",
            maxLength: 191,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(191)",
            oldMaxLength: 191,
            oldNullable: true
        );
    }

    private static void AddImageUrl(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.AddColumn<string>(
            name: "image_url",
            schema: CustomEquipmentDbContext.SchemaName,
            table: table,
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true
        );
    }

    private static void DropColumn(MigrationBuilder migrationBuilder, string table, string column)
    {
        migrationBuilder.DropColumn(
            name: column,
            schema: CustomEquipmentDbContext.SchemaName,
            table: table
        );
    }

    private static void DropConstraint(MigrationBuilder migrationBuilder, string table, string name)
    {
        migrationBuilder.DropCheckConstraint(
            name: name,
            schema: CustomEquipmentDbContext.SchemaName,
            table: table
        );
    }
}
