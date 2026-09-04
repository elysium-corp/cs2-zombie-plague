using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Core.Database.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260904160000_CreateShopModule")]
internal sealed class CreateShopModule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE SCHEMA IF NOT EXISTS shop;

            CREATE TABLE IF NOT EXISTS shop.storefronts (
                shop_type VARCHAR(16) PRIMARY KEY
                    CHECK (shop_type IN ('human', 'zombie')),
                title_key VARCHAR(191) NOT NULL
                    CHECK (title_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$'),
                enabled BOOLEAN NOT NULL DEFAULT TRUE,
                sort_mode VARCHAR(24) NOT NULL DEFAULT 'priority'
                    CHECK (sort_mode IN ('priority', 'price', 'alphabetical')),
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS shop.categories (
                id BIGSERIAL PRIMARY KEY,
                shop_type VARCHAR(16) NOT NULL REFERENCES shop.storefronts(shop_type) ON DELETE CASCADE,
                key VARCHAR(64) NOT NULL,
                display_name_key VARCHAR(191) NOT NULL
                    CHECK (display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$'),
                description_key VARCHAR(191) NULL
                    CHECK (description_key IS NULL OR description_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$'),
                enabled BOOLEAN NOT NULL DEFAULT TRUE,
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT shop_categories_type_key_unique UNIQUE (shop_type, key),
                CONSTRAINT shop_categories_key_valid CHECK (key ~ '^[a-z0-9][a-z0-9_-]{0,63}$')
            );

            CREATE TABLE IF NOT EXISTS shop.offers (
                id BIGSERIAL PRIMARY KEY,
                shop_type VARCHAR(16) NOT NULL REFERENCES shop.storefronts(shop_type) ON DELETE CASCADE,
                provider_key VARCHAR(64) NOT NULL,
                item_key VARCHAR(128) NOT NULL,
                display_name_key VARCHAR(191) NOT NULL
                    CHECK (display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$'),
                category_id BIGINT NULL REFERENCES shop.categories(id) ON DELETE SET NULL,
                description_key VARCHAR(191) NULL
                    CHECK (description_key IS NULL OR description_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$'),
                price INTEGER NOT NULL DEFAULT 0 CHECK (price >= 0),
                ammo_price INTEGER NULL CHECK (ammo_price IS NULL OR ammo_price >= 0),
                ammo_amount INTEGER NOT NULL DEFAULT 1 CHECK (ammo_amount > 0),
                max_purchases_per_round INTEGER NOT NULL DEFAULT 0 CHECK (max_purchases_per_round >= 0),
                max_purchases_per_map INTEGER NOT NULL DEFAULT 0 CHECK (max_purchases_per_map >= 0),
                cooldown_seconds INTEGER NOT NULL DEFAULT 0 CHECK (cooldown_seconds >= 0),
                access_mode VARCHAR(16) NOT NULL DEFAULT 'everyone'
                    CHECK (access_mode IN ('everyone', 'any', 'all')),
                enabled BOOLEAN NOT NULL DEFAULT TRUE,
                sort_order INTEGER NOT NULL DEFAULT 0,
                settings JSONB NOT NULL DEFAULT '{}'::jsonb CHECK (jsonb_typeof(settings) = 'object'),
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT shop_offers_product_unique UNIQUE (shop_type, provider_key, item_key),
                CONSTRAINT shop_offers_provider_valid CHECK (provider_key ~ '^[a-z0-9][a-z0-9_-]{0,63}$')
            );

            CREATE TABLE IF NOT EXISTS shop.offer_privileges (
                offer_id BIGINT NOT NULL REFERENCES shop.offers(id) ON DELETE CASCADE,
                privilege_key VARCHAR(129) NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                PRIMARY KEY (offer_id, privilege_key),
                CONSTRAINT shop_offer_privilege_key_valid
                    CHECK (privilege_key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,128}$')
            );

            CREATE TABLE IF NOT EXISTS shop.fallback_state (
                id SMALLINT PRIMARY KEY CHECK (id = 1),
                dirty BOOLEAN NOT NULL DEFAULT TRUE
            );

            INSERT INTO shop.storefronts (shop_type, title_key, enabled, sort_mode)
            VALUES
                ('human', 'Shop.Human.Title', TRUE, 'priority'),
                ('zombie', 'Shop.Zombie.Title', TRUE, 'priority')
            ON CONFLICT (shop_type) DO NOTHING;

            INSERT INTO shop.fallback_state (id, dirty)
            VALUES (1, TRUE)
            ON CONFLICT (id) DO NOTHING;

            CREATE INDEX IF NOT EXISTS ix_shop_categories_runtime
                ON shop.categories (shop_type, enabled, sort_order, id);
            CREATE INDEX IF NOT EXISTS ix_shop_offers_runtime
                ON shop.offers (shop_type, enabled, sort_order, id);

            DO $migration$
            BEGIN
                IF to_regclass('custom_equipment.shop_settings') IS NOT NULL THEN
                    INSERT INTO shop.storefronts (shop_type, title_key, enabled, sort_mode)
                    SELECT shop_type, display_name_key, enabled, 'priority'
                    FROM custom_equipment.shop_settings
                    ON CONFLICT (shop_type) DO UPDATE SET
                        title_key = EXCLUDED.title_key,
                        enabled = EXCLUDED.enabled;
                END IF;

                IF to_regclass('custom_equipment.shop_categories') IS NOT NULL THEN
                    INSERT INTO shop.categories (
                        id, shop_type, key, display_name_key, description_key, enabled, sort_order)
                    SELECT id, shop_type, key, display_name_key, description_key, enabled, sort_order
                    FROM custom_equipment.shop_categories
                    ON CONFLICT (id) DO NOTHING;
                END IF;

                IF to_regclass('custom_equipment.shop_listings') IS NOT NULL THEN
                    INSERT INTO shop.offers (
                        id, shop_type, provider_key, item_key, display_name_key, category_id,
                        description_key, price, ammo_price, ammo_amount,
                        max_purchases_per_round, max_purchases_per_map, cooldown_seconds,
                        access_mode, enabled, sort_order, settings)
                    SELECT
                        listing.id,
                        listing.shop_type,
                        CASE WHEN listing.item_internal_name = 'custom_equipment:armor'
                            THEN 'builtin' ELSE 'custom_equipment' END,
                        CASE WHEN listing.item_internal_name = 'custom_equipment:armor'
                            THEN 'armor' ELSE listing.item_internal_name END,
                        COALESCE(
                            product.display_name_key,
                            weapon.display_name_key,
                            gameplay.display_name_key,
                            'Shop.Item.Unknown.Name'),
                        listing.category_id,
                        listing.description_key,
                        listing.price,
                        CASE WHEN weapon.id IS NULL THEN NULL ELSE weapon.ammo_price END,
                        1,
                        listing.max_purchases_per_round,
                        listing.max_purchases_per_map,
                        0,
                        'everyone',
                        listing.enabled,
                        listing.sort_order,
                        listing.settings
                    FROM custom_equipment.shop_listings listing
                    LEFT JOIN custom_equipment.shop_products product
                        ON product.internal_name = listing.item_internal_name
                    LEFT JOIN custom_equipment.weapons weapon
                        ON weapon.internal_name = listing.item_internal_name
                    LEFT JOIN custom_equipment.gameplay_items gameplay
                        ON gameplay.internal_name = listing.item_internal_name
                    ON CONFLICT (id) DO NOTHING;
                END IF;

                PERFORM setval(
                    pg_get_serial_sequence('shop.categories', 'id'),
                    GREATEST(COALESCE((SELECT MAX(id) FROM shop.categories), 1), 1),
                    EXISTS (SELECT 1 FROM shop.categories));
                PERFORM setval(
                    pg_get_serial_sequence('shop.offers', 'id'),
                    GREATEST(COALESCE((SELECT MAX(id) FROM shop.offers), 1), 1),
                    EXISTS (SELECT 1 FROM shop.offers));
            END
            $migration$;

            CREATE OR REPLACE FUNCTION shop.mark_fallback_dirty()
            RETURNS TRIGGER
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                INSERT INTO shop.fallback_state (id, dirty)
                VALUES (1, TRUE)
                ON CONFLICT (id) DO UPDATE SET dirty = TRUE;
                RETURN NULL;
            END
            $function$;

            DO $triggers$
            DECLARE
                target_table TEXT;
                trigger_name TEXT;
            BEGIN
                FOREACH target_table IN ARRAY ARRAY[
                    'shop.storefronts',
                    'shop.categories',
                    'shop.offers',
                    'shop.offer_privileges'
                ]
                LOOP
                    IF to_regclass(target_table) IS NULL THEN
                        CONTINUE;
                    END IF;

                    trigger_name := 'mark_shop_fallback_dirty_' || replace(target_table, '.', '_');
                    EXECUTE format('DROP TRIGGER IF EXISTS %I ON %s', trigger_name, target_table);
                    EXECUTE format(
                        'CREATE TRIGGER %I AFTER INSERT OR UPDATE OR DELETE OR TRUNCATE ON %s '
                        'FOR EACH STATEMENT EXECUTE FUNCTION shop.mark_fallback_dirty()',
                        trigger_name,
                        target_table);
                END LOOP;
            END
            $triggers$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP SCHEMA IF EXISTS shop CASCADE;
            """);
    }
}
