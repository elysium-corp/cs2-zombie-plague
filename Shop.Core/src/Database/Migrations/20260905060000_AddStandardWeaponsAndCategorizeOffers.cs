using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shop.Core.Database.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20260905060000_AddStandardWeaponsAndCategorizeOffers")]
internal sealed class AddStandardWeaponsAndCategorizeOffers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE shop.standard_weapons (
                item_key VARCHAR(128) PRIMARY KEY,
                display_name_key VARCHAR(191) NOT NULL
                    CHECK (display_name_key ~ '^[A-Z0-9][A-Za-z0-9]*(\.[A-Z0-9][A-Za-z0-9]*)*$'),
                category_key VARCHAR(64) NOT NULL
                    CHECK (category_key IN ('pistol', 'submachine_gun', 'rifle',
                        'sniper_rifle', 'shotgun', 'machine_gun')),
                default_price INTEGER NOT NULL CHECK (default_price > 0)
            );

            INSERT INTO shop.standard_weapons (item_key, display_name_key, category_key, default_price)
            VALUES
                ('weapon_glock', 'Shop.Weapon.Glock.Name', 'pistol', 200),
                ('weapon_hkp2000', 'Shop.Weapon.P2000.Name', 'pistol', 200),
                ('weapon_usp_silencer', 'Shop.Weapon.UspS.Name', 'pistol', 200),
                ('weapon_elite', 'Shop.Weapon.DualBerettas.Name', 'pistol', 300),
                ('weapon_p250', 'Shop.Weapon.P250.Name', 'pistol', 300),
                ('weapon_tec9', 'Shop.Weapon.Tec9.Name', 'pistol', 500),
                ('weapon_fiveseven', 'Shop.Weapon.FiveSeven.Name', 'pistol', 500),
                ('weapon_cz75a', 'Shop.Weapon.Cz75Auto.Name', 'pistol', 500),
                ('weapon_deagle', 'Shop.Weapon.DesertEagle.Name', 'pistol', 700),
                ('weapon_revolver', 'Shop.Weapon.R8Revolver.Name', 'pistol', 600),
                ('weapon_mac10', 'Shop.Weapon.Mac10.Name', 'submachine_gun', 1050),
                ('weapon_mp9', 'Shop.Weapon.Mp9.Name', 'submachine_gun', 1250),
                ('weapon_mp7', 'Shop.Weapon.Mp7.Name', 'submachine_gun', 1500),
                ('weapon_mp5sd', 'Shop.Weapon.Mp5Sd.Name', 'submachine_gun', 1500),
                ('weapon_ump45', 'Shop.Weapon.Ump45.Name', 'submachine_gun', 1200),
                ('weapon_p90', 'Shop.Weapon.P90.Name', 'submachine_gun', 2350),
                ('weapon_bizon', 'Shop.Weapon.PpBizon.Name', 'submachine_gun', 1400),
                ('weapon_galilar', 'Shop.Weapon.GalilAr.Name', 'rifle', 1800),
                ('weapon_famas', 'Shop.Weapon.Famas.Name', 'rifle', 1950),
                ('weapon_ak47', 'Shop.Weapon.Ak47.Name', 'rifle', 2700),
                ('weapon_m4a1', 'Shop.Weapon.M4A4.Name', 'rifle', 2900),
                ('weapon_m4a1_silencer', 'Shop.Weapon.M4A1S.Name', 'rifle', 2900),
                ('weapon_aug', 'Shop.Weapon.Aug.Name', 'rifle', 3300),
                ('weapon_sg556', 'Shop.Weapon.Sg553.Name', 'rifle', 3000),
                ('weapon_ssg08', 'Shop.Weapon.Ssg08.Name', 'sniper_rifle', 1700),
                ('weapon_awp', 'Shop.Weapon.Awp.Name', 'sniper_rifle', 4750),
                ('weapon_scar20', 'Shop.Weapon.Scar20.Name', 'sniper_rifle', 5000),
                ('weapon_g3sg1', 'Shop.Weapon.G3Sg1.Name', 'sniper_rifle', 5000),
                ('weapon_nova', 'Shop.Weapon.Nova.Name', 'shotgun', 1050),
                ('weapon_xm1014', 'Shop.Weapon.Xm1014.Name', 'shotgun', 2000),
                ('weapon_mag7', 'Shop.Weapon.Mag7.Name', 'shotgun', 1300),
                ('weapon_sawedoff', 'Shop.Weapon.SawedOff.Name', 'shotgun', 1100),
                ('weapon_m249', 'Shop.Weapon.M249.Name', 'machine_gun', 5200),
                ('weapon_negev', 'Shop.Weapon.Negev.Name', 'machine_gun', 1700);

            CREATE TEMP TABLE shop_category_seed (
                key VARCHAR(64) PRIMARY KEY,
                display_name_key VARCHAR(191) NOT NULL,
                sort_order INTEGER NOT NULL
            ) ON COMMIT DROP;

            INSERT INTO shop_category_seed (key, display_name_key, sort_order) VALUES
                ('pistol', 'Menu.Equipment.Category.Pistol', 0),
                ('submachine_gun', 'Menu.Equipment.Category.SubmachineGun', 10),
                ('rifle', 'Menu.Equipment.Category.Rifle', 20),
                ('shotgun', 'Menu.Equipment.Category.Shotgun', 30),
                ('sniper_rifle', 'Menu.Equipment.Category.SniperRifle', 40),
                ('machine_gun', 'Menu.Equipment.Category.MachineGun', 50),
                ('grenade', 'Menu.Equipment.Category.Grenade', 60),
                ('equipment', 'Menu.Equipment.Category.Equipment', 70);

            CREATE TEMP TABLE shop_offer_category_seed ON COMMIT DROP AS
            SELECT id AS offer_id, shop_type, 'equipment'::VARCHAR(64) AS category_key
            FROM shop.offers
            WHERE category_id IS NULL;

            DO $categorize$
            BEGIN
                IF to_regclass('custom_equipment.weapons') IS NOT NULL THEN
                    UPDATE shop_offer_category_seed AS seed
                    SET category_key = COALESCE(standard.category_key, CASE lower(weapon.weapon_type)
                        WHEN 'pistol' THEN 'pistol'
                        WHEN 'submachinegun' THEN 'submachine_gun'
                        WHEN 'rifle' THEN 'rifle'
                        WHEN 'sniperrifle' THEN 'sniper_rifle'
                        WHEN 'shotgun' THEN 'shotgun'
                        WHEN 'machinegun' THEN 'machine_gun'
                        WHEN 'grenade' THEN 'grenade'
                        ELSE 'equipment' END)
                    FROM shop.offers AS offer
                    JOIN custom_equipment.weapons AS weapon
                      ON weapon.internal_name = offer.item_key
                    LEFT JOIN shop.standard_weapons AS standard
                      ON standard.item_key = 'weapon_' ||
                          regexp_replace(lower(trim(weapon.inheritor_name)), '^weapon_', '')
                    WHERE seed.offer_id = offer.id AND offer.provider_key = 'custom_equipment';
                END IF;

                IF to_regclass('custom_equipment.gameplay_items') IS NOT NULL THEN
                    UPDATE shop_offer_category_seed AS seed
                    SET category_key = CASE WHEN gameplay.implementation_key IN (
                        'barrier_nade', 'fire_nade', 'frost_nade', 'jump_nade', 'shake_nade')
                        THEN 'grenade' ELSE 'equipment' END
                    FROM shop.offers AS offer
                    JOIN custom_equipment.gameplay_items AS gameplay
                      ON gameplay.internal_name = offer.item_key
                    WHERE seed.offer_id = offer.id AND offer.provider_key = 'custom_equipment';
                END IF;
            END
            $categorize$;

            UPDATE shop_offer_category_seed AS seed
            SET category_key = standard.category_key
            FROM shop.offers AS offer
            JOIN shop.standard_weapons AS standard ON standard.item_key = offer.item_key
            WHERE seed.offer_id = offer.id AND offer.provider_key = 'cs2_weapon';

            INSERT INTO shop.categories (shop_type, key, display_name_key, sort_order)
            SELECT desired.shop_type, category.key, category.display_name_key, category.sort_order
            FROM (
                SELECT shop_type, category_key FROM shop_offer_category_seed
                UNION
                SELECT 'human', category_key FROM shop.standard_weapons
            ) AS desired
            JOIN shop_category_seed AS category ON category.key = desired.category_key
            ON CONFLICT (shop_type, key) DO NOTHING;

            UPDATE shop.offers AS offer
            SET category_id = category.id, updated_at = NOW()
            FROM shop_offer_category_seed AS seed
            JOIN shop.categories AS category
              ON category.shop_type = seed.shop_type AND category.key = seed.category_key
            WHERE offer.id = seed.offer_id AND offer.category_id IS NULL;

            INSERT INTO shop.offers (
                shop_type, provider_key, item_key, display_name_key, category_id, price, sort_order)
            SELECT 'human', 'cs2_weapon', weapon.item_key, weapon.display_name_key,
                   category.id, weapon.default_price, 0
            FROM shop.standard_weapons AS weapon
            JOIN shop.categories AS category
              ON category.shop_type = 'human' AND category.key = weapon.category_key
            ORDER BY category.sort_order, weapon.default_price, weapon.item_key
            ON CONFLICT (shop_type, provider_key, item_key) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Товары и категории могли быть настроены администратором после обновления.
        // Их удаление или перенос в корень при откате привели бы к потере этих настроек.
        migrationBuilder.Sql("DROP TABLE shop.standard_weapons;");
    }
}
