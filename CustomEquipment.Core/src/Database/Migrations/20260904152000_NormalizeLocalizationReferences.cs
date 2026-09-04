using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomEquipment.Database.Migrations;

[DbContext(typeof(CustomEquipmentDbContext))]
[Migration("20260904152000_NormalizeLocalizationReferences")]
internal sealed class NormalizeLocalizationReferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION custom_equipment.canonicalize_localization_key(source TEXT)
            RETURNS TEXT
            LANGUAGE SQL
            IMMUTABLE
            STRICT
            PARALLEL SAFE
            AS $function$
                SELECT string_agg(
                    CASE
                        WHEN part.ordinality = 1 AND lower(part.value) = 'tags' THEN 'Tag'
                        ELSE upper(left(part.value, 1)) || substr(part.value, 2)
                    END,
                    '.' ORDER BY part.ordinality
                )
                FROM unnest(regexp_split_to_array(btrim(source), '[._[:space:]-]+'))
                    WITH ORDINALITY AS part(value, ordinality)
                WHERE part.value <> ''
            $function$;

            ALTER TABLE custom_equipment.weapons
                DROP CONSTRAINT IF EXISTS "CK_weapons_display_name_key";
            ALTER TABLE custom_equipment.gameplay_items
                DROP CONSTRAINT IF EXISTS "CK_gameplay_items_display_name_key";
            ALTER TABLE custom_equipment.shop_settings
                DROP CONSTRAINT IF EXISTS "CK_shop_settings_display_name_key";
            ALTER TABLE custom_equipment.shop_categories
                DROP CONSTRAINT IF EXISTS "CK_shop_categories_localization_keys";
            ALTER TABLE custom_equipment.shop_listings
                DROP CONSTRAINT IF EXISTS "CK_shop_listings_description_key";
            ALTER TABLE custom_equipment.shop_products
                DROP CONSTRAINT IF EXISTS "CK_shop_products_display_name_key";

            UPDATE custom_equipment.weapons
            SET display_name_key = custom_equipment.canonicalize_localization_key(display_name_key),
                updated_at = NOW();
            UPDATE custom_equipment.gameplay_items
            SET display_name_key = custom_equipment.canonicalize_localization_key(display_name_key),
                updated_at = NOW();
            UPDATE custom_equipment.shop_settings
            SET display_name_key = custom_equipment.canonicalize_localization_key(display_name_key),
                updated_at = NOW();
            UPDATE custom_equipment.shop_categories
            SET display_name_key = custom_equipment.canonicalize_localization_key(display_name_key),
                description_key = CASE
                    WHEN description_key IS NULL THEN NULL
                    ELSE custom_equipment.canonicalize_localization_key(description_key)
                END,
                updated_at = NOW();
            UPDATE custom_equipment.shop_listings
            SET description_key = CASE
                    WHEN description_key IS NULL THEN NULL
                    ELSE custom_equipment.canonicalize_localization_key(description_key)
                END,
                updated_at = NOW();
            UPDATE custom_equipment.shop_products
            SET display_name_key = custom_equipment.canonicalize_localization_key(display_name_key),
                updated_at = NOW();

            ALTER TABLE custom_equipment.weapons
                ADD CONSTRAINT "CK_weapons_display_name_key"
                CHECK (display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$');
            ALTER TABLE custom_equipment.gameplay_items
                ADD CONSTRAINT "CK_gameplay_items_display_name_key"
                CHECK (display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$');
            ALTER TABLE custom_equipment.shop_settings
                ADD CONSTRAINT "CK_shop_settings_display_name_key"
                CHECK (display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$');
            ALTER TABLE custom_equipment.shop_categories
                ADD CONSTRAINT "CK_shop_categories_localization_keys"
                CHECK (
                    display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$'
                    AND (description_key IS NULL OR description_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$')
                );
            ALTER TABLE custom_equipment.shop_listings
                ADD CONSTRAINT "CK_shop_listings_description_key"
                CHECK (description_key IS NULL OR description_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$');
            ALTER TABLE custom_equipment.shop_products
                ADD CONSTRAINT "CK_shop_products_display_name_key"
                CHECK (display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$');

            DROP FUNCTION custom_equipment.canonicalize_localization_key(TEXT);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE custom_equipment.weapons
                DROP CONSTRAINT IF EXISTS "CK_weapons_display_name_key";
            ALTER TABLE custom_equipment.gameplay_items
                DROP CONSTRAINT IF EXISTS "CK_gameplay_items_display_name_key";
            ALTER TABLE custom_equipment.shop_settings
                DROP CONSTRAINT IF EXISTS "CK_shop_settings_display_name_key";
            ALTER TABLE custom_equipment.shop_categories
                DROP CONSTRAINT IF EXISTS "CK_shop_categories_localization_keys";
            ALTER TABLE custom_equipment.shop_listings
                DROP CONSTRAINT IF EXISTS "CK_shop_listings_description_key";
            ALTER TABLE custom_equipment.shop_products
                DROP CONSTRAINT IF EXISTS "CK_shop_products_display_name_key";

            ALTER TABLE custom_equipment.weapons
                ADD CONSTRAINT "CK_weapons_display_name_key"
                CHECK (display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$');
            ALTER TABLE custom_equipment.gameplay_items
                ADD CONSTRAINT "CK_gameplay_items_display_name_key"
                CHECK (display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$');
            ALTER TABLE custom_equipment.shop_settings
                ADD CONSTRAINT "CK_shop_settings_display_name_key"
                CHECK (display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$');
            ALTER TABLE custom_equipment.shop_categories
                ADD CONSTRAINT "CK_shop_categories_localization_keys"
                CHECK (display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$' AND (description_key IS NULL OR description_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$'));
            ALTER TABLE custom_equipment.shop_listings
                ADD CONSTRAINT "CK_shop_listings_description_key"
                CHECK (description_key IS NULL OR description_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$');
            ALTER TABLE custom_equipment.shop_products
                ADD CONSTRAINT "CK_shop_products_display_name_key"
                CHECK (display_name_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$');
            """);
    }
}
