using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260831010000_InitialLocalization")]
internal sealed class InitialLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE SCHEMA IF NOT EXISTS localization;

            CREATE TABLE IF NOT EXISTS localization.languages (
                id BIGSERIAL PRIMARY KEY,
                code VARCHAR(16) NOT NULL UNIQUE,
                name VARCHAR(64) NOT NULL,
                native_name VARCHAR(64) NOT NULL,
                enabled BOOLEAN NOT NULL DEFAULT TRUE,
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT languages_code_format CHECK (code ~ '^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})?$')
            );

            INSERT INTO localization.languages (code, name, native_name, enabled, sort_order)
            VALUES
                ('ru', 'Русский', 'Русский', TRUE, 10),
                ('en', 'English', 'English', TRUE, 20),
                ('de', 'Deutsch', 'Deutsch', TRUE, 30),
                ('pl', 'Polski', 'Polski', TRUE, 40)
            ON CONFLICT (code) DO NOTHING;

            CREATE TABLE IF NOT EXISTS localization.settings (
                id SMALLINT PRIMARY KEY DEFAULT 1 CHECK (id = 1),
                server_fallback_language VARCHAR(16) NOT NULL
                    REFERENCES localization.languages(code) ON DELETE RESTRICT ON UPDATE CASCADE,
                refresh_interval_seconds INTEGER NOT NULL DEFAULT 30
                    CHECK (refresh_interval_seconds >= 5),
                local_cache_enabled BOOLEAN NOT NULL DEFAULT TRUE,
                log_missing_keys BOOLEAN NOT NULL DEFAULT TRUE,
                configuration_version BIGINT NOT NULL DEFAULT 1 CHECK (configuration_version > 0),
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            INSERT INTO localization.settings (id, server_fallback_language)
            VALUES (1, 'ru')
            ON CONFLICT (id) DO NOTHING;

            CREATE TABLE IF NOT EXISTS localization.entries (
                id BIGSERIAL PRIMARY KEY,
                key VARCHAR(191) NOT NULL UNIQUE,
                description VARCHAR(512) NULL,
                is_critical BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT entries_key_format CHECK (key ~ '^[A-Za-z0-9][A-Za-z0-9_.-]{1,190}$')
            );

            CREATE TABLE IF NOT EXISTS localization.translations (
                entry_id BIGINT NOT NULL REFERENCES localization.entries(id) ON DELETE CASCADE,
                language_code VARCHAR(16) NOT NULL
                    REFERENCES localization.languages(code) ON DELETE CASCADE ON UPDATE CASCADE,
                text TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                PRIMARY KEY (entry_id, language_code),
                CONSTRAINT translations_text_not_blank CHECK (btrim(text) <> '')
            );

            CREATE INDEX IF NOT EXISTS translations_language_idx
                ON localization.translations (language_code);

            CREATE TABLE IF NOT EXISTS localization.player_preferences (
                steam_id BIGINT PRIMARY KEY,
                language_code VARCHAR(16) NOT NULL
                    REFERENCES localization.languages(code) ON DELETE CASCADE ON UPDATE CASCADE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TEMP TABLE localization_seed (
                key VARCHAR(191) NOT NULL,
                is_critical BOOLEAN NOT NULL,
                language_code VARCHAR(16) NOT NULL,
                text TEXT NOT NULL
            ) ON COMMIT DROP;

            INSERT INTO localization_seed (key, is_critical, language_code, text)
            VALUES
                ('localization.menu.title', TRUE, 'ru', 'Язык / Language'),
                ('localization.menu.title', TRUE, 'en', 'Language'),
                ('localization.menu.title', TRUE, 'de', 'Sprache'),
                ('localization.menu.title', TRUE, 'pl', 'Język'),
                ('localization.menu.changed', TRUE, 'ru', 'Язык изменён на {language}'),
                ('localization.menu.changed', TRUE, 'en', 'Language changed to {language}'),
                ('localization.menu.changed', TRUE, 'de', 'Sprache geändert zu {language}'),
                ('localization.menu.changed', TRUE, 'pl', 'Zmieniono język na {language}'),
                ('localization.menu.loading', TRUE, 'ru', 'Локализация ещё загружается'),
                ('localization.menu.loading', TRUE, 'en', 'Localization is still loading'),
                ('localization.menu.loading', TRUE, 'de', 'Die Lokalisierung wird noch geladen'),
                ('localization.menu.loading', TRUE, 'pl', 'Lokalizacja wciąż się ładuje'),
                ('localization.menu.unavailable', TRUE, 'ru', 'Не удалось изменить язык. Попробуйте позже'),
                ('localization.menu.unavailable', TRUE, 'en', 'Unable to change language. Try again later'),
                ('localization.menu.unavailable', TRUE, 'de', 'Die Sprache konnte nicht geändert werden. Versuche es später erneut'),
                ('localization.menu.unavailable', TRUE, 'pl', 'Nie udało się zmienić języka. Spróbuj ponownie później'),
                ('advertisement.messages.discord', FALSE, 'ru', 'Наш Discord: {accent}discord.gg/elysium{/accent}'),
                ('advertisement.messages.discord', FALSE, 'en', 'Our Discord: {accent}discord.gg/elysium{/accent}'),
                ('ResetScore.ResetMessage', FALSE, 'ru', 'Ваш счёт обнулён!'),
                ('ResetScore.ResetMessage', FALSE, 'en', 'Your score has been reset!'),
                ('DamageNotify.HitMessage', FALSE, 'ru', 'Вы попали в'),
                ('DamageNotify.HitMessage', FALSE, 'en', 'You hit'),
                ('Statistics.PointsGained', FALSE, 'ru', 'За прошлый раунд вы получили +{points} очков.'),
                ('Statistics.PointsGained', FALSE, 'en', 'You gained +{points} points last round.'),
                ('Statistics.PointsLost', FALSE, 'ru', 'За прошлый раунд вы потеряли {points} очков.'),
                ('Statistics.PointsLost', FALSE, 'en', 'You lost {points} points last round.'),
                ('Statistics.PointsUnchanged', FALSE, 'ru', 'За прошлый раунд ваши очки не изменились.'),
                ('Statistics.PointsUnchanged', FALSE, 'en', 'Your points did not change last round.'),
                ('Menu.Main.Title', FALSE, 'ru', 'Меню сервера'),
                ('Menu.Main.Title', FALSE, 'en', 'Server menu'),
                ('Menu.Main.Item.ZClass.Title', FALSE, 'ru', 'Выбрать класс зомби'),
                ('Menu.Main.Item.ZClass.Title', FALSE, 'en', 'Select zombie class'),
                ('Menu.ZClass.Title', FALSE, 'ru', 'Классы зомби'),
                ('Menu.ZClass.Title', FALSE, 'en', 'Zombie classes'),
                ('Menu.ZClass.Selected', FALSE, 'ru', '{class} [выбран]'),
                ('Menu.ZClass.Selected', FALSE, 'en', '{class} [selected]'),
                ('Menu.ZClass.SelectionSuccess', FALSE, 'ru', 'Вы успешно выбрали класс зомби: {class}'),
                ('Menu.ZClass.SelectionSuccess', FALSE, 'en', 'Zombie class selected: {class}'),
                ('Menu.Main.Item.Knife.Title', FALSE, 'ru', 'Выбрать нож'),
                ('Menu.Main.Item.Knife.Title', FALSE, 'en', 'Select knife'),
                ('Menu.Knife.Title', FALSE, 'ru', 'Ножи'),
                ('Menu.Knife.Title', FALSE, 'en', 'Knives'),
                ('Menu.Knife.Selected', FALSE, 'ru', '{knife} [выбран]'),
                ('Menu.Knife.Selected', FALSE, 'en', '{knife} [selected]'),
                ('Menu.Knife.SelectionSuccess', FALSE, 'ru', 'Вы выбрали нож: {knife}'),
                ('Menu.Knife.SelectionSuccess', FALSE, 'en', 'Knife selected: {knife}'),
                ('CustomKnife.Monarch.Name', FALSE, 'ru', 'Монарх'),
                ('CustomKnife.Monarch.Name', FALSE, 'en', 'Monarch'),
                ('Menu.Main.Item.Equipment.Title', FALSE, 'ru', 'Магазин снаряжения'),
                ('Menu.Main.Item.Equipment.Title', FALSE, 'en', 'Equipment Shop'),
                ('Menu.Equipment.Title', FALSE, 'ru', 'Снаряжение'),
                ('Menu.Equipment.Title', FALSE, 'en', 'Equipment'),
                ('Menu.Equipment.Category.Pistol', FALSE, 'ru', 'Пистолеты'),
                ('Menu.Equipment.Category.Pistol', FALSE, 'en', 'Pistols'),
                ('Menu.Equipment.Category.SubmachineGun', FALSE, 'ru', 'Пистолеты-пулемёты'),
                ('Menu.Equipment.Category.SubmachineGun', FALSE, 'en', 'Submachine Guns'),
                ('Menu.Equipment.Category.Rifle', FALSE, 'ru', 'Штурмовые винтовки'),
                ('Menu.Equipment.Category.Rifle', FALSE, 'en', 'Rifles'),
                ('Menu.Equipment.Category.Shotgun', FALSE, 'ru', 'Дробовики'),
                ('Menu.Equipment.Category.Shotgun', FALSE, 'en', 'Shotguns'),
                ('Menu.Equipment.Category.SniperRifle', FALSE, 'ru', 'Снайперские винтовки'),
                ('Menu.Equipment.Category.SniperRifle', FALSE, 'en', 'Sniper Rifles'),
                ('Menu.Equipment.Category.MachineGun', FALSE, 'ru', 'Пулемёты'),
                ('Menu.Equipment.Category.MachineGun', FALSE, 'en', 'Machine Guns'),
                ('Menu.Equipment.Category.Grenade', FALSE, 'ru', 'Гранаты'),
                ('Menu.Equipment.Category.Grenade', FALSE, 'en', 'Grenades'),
                ('Menu.Equipment.Category.Equipment', FALSE, 'ru', 'Экипировка'),
                ('Menu.Equipment.Category.Equipment', FALSE, 'en', 'Equipment'),
                ('Equipment.Errors.RoleUnavailable', FALSE, 'ru', 'Этот предмет недоступен для текущей роли!'),
                ('Equipment.Errors.RoleUnavailable', FALSE, 'en', 'This item is unavailable for your current role!'),
                ('Equipment.Errors.NotEnoughMoney', FALSE, 'ru', 'Недостаточно денег!'),
                ('Equipment.Errors.NotEnoughMoney', FALSE, 'en', 'Not enough money!'),
                ('Ammo.Warning.NotEnoughMoney', FALSE, 'ru', 'Не хватает денег'),
                ('Ammo.Warning.NotEnoughMoney', FALSE, 'en', 'You don''t have enough money'),
                ('Ammo.Warning.EnoughAmmo', FALSE, 'ru', 'Боезапас заполнен'),
                ('Ammo.Warning.EnoughAmmo', FALSE, 'en', 'Ammo full'),
                ('RoundRatingNotify.prefix', FALSE, 'ru', '[[green]Elysium[default]]'),
                ('RoundRatingNotify.prefix', FALSE, 'en', '[[green]Elysium[default]]'),
                ('RoundRatingNotify.HumanTop', FALSE, 'ru', 'Лучший игрок за людей: {player} — нанёс {value} урона.'),
                ('RoundRatingNotify.HumanTop', FALSE, 'en', 'Best human player: {player} — dealt {value} damage.'),
                ('RoundRatingNotify.ZombieTop', FALSE, 'ru', 'Лучший игрок за зомби: {player} — заразил {value} игроков.'),
                ('RoundRatingNotify.ZombieTop', FALSE, 'en', 'Best zombie player: {player} — infected {value} players.');

            INSERT INTO localization.entries (key, is_critical)
            SELECT key, BOOL_OR(is_critical)
            FROM localization_seed
            GROUP BY key
            ON CONFLICT (key) DO UPDATE
                SET is_critical = localization.entries.is_critical OR EXCLUDED.is_critical;

            INSERT INTO localization.translations (entry_id, language_code, text)
            SELECT entry.id, seed.language_code, seed.text
            FROM localization_seed AS seed
            JOIN localization.entries AS entry ON entry.key = seed.key
            ON CONFLICT (entry_id, language_code) DO NOTHING;

            DO $migration$
            BEGIN
                IF to_regclass('advertisement.messages') IS NOT NULL
                   AND to_regclass('advertisement.message_translations') IS NOT NULL THEN
                    EXECUTE $sql$
                        INSERT INTO localization.entries (key, description)
                        SELECT 'advertisement.messages.' || message.key, message.name
                        FROM advertisement.messages AS message
                        ON CONFLICT (key) DO UPDATE
                            SET description = COALESCE(EXCLUDED.description, localization.entries.description)
                    $sql$;

                    EXECUTE $sql$
                        INSERT INTO localization.translations (entry_id, language_code, text)
                        SELECT entry.id, lower(translation.locale), translation.text
                        FROM advertisement.message_translations AS translation
                        JOIN advertisement.messages AS message ON message.id = translation.message_id
                        JOIN localization.entries AS entry
                          ON entry.key = 'advertisement.messages.' || message.key
                        JOIN localization.languages AS language
                          ON language.code = lower(translation.locale)
                        ON CONFLICT (entry_id, language_code) DO UPDATE SET
                            text = EXCLUDED.text,
                            updated_at = NOW()
                    $sql$;
                END IF;

                IF to_regclass('core.player_preferences') IS NOT NULL
                   AND EXISTS (
                       SELECT 1
                       FROM information_schema.columns
                       WHERE table_schema = 'core'
                         AND table_name = 'player_preferences'
                         AND column_name = 'locale'
                   ) THEN
                    EXECUTE $sql$
                        INSERT INTO localization.player_preferences
                            (steam_id, language_code, created_at, updated_at)
                        SELECT preference.steam_id, language.code, NOW(), preference.updated_at
                        FROM core.player_preferences AS preference
                        JOIN localization.languages AS language
                          ON language.code = lower(preference.locale)
                        WHERE preference.locale IS NOT NULL
                        ON CONFLICT (steam_id) DO NOTHING
                    $sql$;
                END IF;
            END
            $migration$;

            CREATE OR REPLACE FUNCTION localization.touch_updated_at()
            RETURNS TRIGGER LANGUAGE plpgsql AS $function$
            BEGIN
                NEW.updated_at = NOW();
                RETURN NEW;
            END
            $function$;

            CREATE OR REPLACE FUNCTION localization.bump_configuration_version()
            RETURNS TRIGGER LANGUAGE plpgsql AS $function$
            BEGIN
                UPDATE localization.settings
                SET configuration_version = configuration_version + 1,
                    updated_at = NOW()
                WHERE id = 1;
                RETURN NULL;
            END
            $function$;

            CREATE OR REPLACE FUNCTION localization.protect_fallback_language()
            RETURNS TRIGGER LANGUAGE plpgsql AS $function$
            DECLARE
                fallback_code VARCHAR(16);
            BEGIN
                SELECT server_fallback_language INTO fallback_code
                FROM localization.settings
                WHERE id = 1;

                IF TG_OP = 'DELETE' AND OLD.code = fallback_code THEN
                    RAISE EXCEPTION 'Нельзя удалить текущий fallback-язык %', OLD.code;
                END IF;

                IF TG_OP = 'UPDATE'
                   AND OLD.code = fallback_code
                   AND (NEW.code <> OLD.code OR NOT NEW.enabled) THEN
                    RAISE EXCEPTION 'Сначала выберите другой fallback-язык';
                END IF;

                IF TG_OP = 'DELETE' THEN
                    RETURN OLD;
                END IF;

                RETURN NEW;
            END
            $function$;

            DROP TRIGGER IF EXISTS languages_touch_updated_at ON localization.languages;
            CREATE TRIGGER languages_touch_updated_at
                BEFORE UPDATE ON localization.languages
                FOR EACH ROW EXECUTE FUNCTION localization.touch_updated_at();
            DROP TRIGGER IF EXISTS entries_touch_updated_at ON localization.entries;
            CREATE TRIGGER entries_touch_updated_at
                BEFORE UPDATE ON localization.entries
                FOR EACH ROW EXECUTE FUNCTION localization.touch_updated_at();
            DROP TRIGGER IF EXISTS translations_touch_updated_at ON localization.translations;
            CREATE TRIGGER translations_touch_updated_at
                BEFORE UPDATE ON localization.translations
                FOR EACH ROW EXECUTE FUNCTION localization.touch_updated_at();
            DROP TRIGGER IF EXISTS preferences_touch_updated_at ON localization.player_preferences;
            CREATE TRIGGER preferences_touch_updated_at
                BEFORE UPDATE ON localization.player_preferences
                FOR EACH ROW EXECUTE FUNCTION localization.touch_updated_at();
            DROP TRIGGER IF EXISTS settings_touch_updated_at ON localization.settings;
            CREATE TRIGGER settings_touch_updated_at
                BEFORE UPDATE ON localization.settings
                FOR EACH ROW EXECUTE FUNCTION localization.touch_updated_at();

            DROP TRIGGER IF EXISTS languages_bump_localization_version ON localization.languages;
            CREATE TRIGGER languages_bump_localization_version
                AFTER INSERT OR UPDATE OR DELETE ON localization.languages
                FOR EACH STATEMENT EXECUTE FUNCTION localization.bump_configuration_version();
            DROP TRIGGER IF EXISTS entries_bump_localization_version ON localization.entries;
            CREATE TRIGGER entries_bump_localization_version
                AFTER INSERT OR UPDATE OR DELETE ON localization.entries
                FOR EACH STATEMENT EXECUTE FUNCTION localization.bump_configuration_version();
            DROP TRIGGER IF EXISTS translations_bump_localization_version ON localization.translations;
            CREATE TRIGGER translations_bump_localization_version
                AFTER INSERT OR UPDATE OR DELETE ON localization.translations
                FOR EACH STATEMENT EXECUTE FUNCTION localization.bump_configuration_version();

            DROP TRIGGER IF EXISTS protect_fallback_language ON localization.languages;
            CREATE TRIGGER protect_fallback_language
                BEFORE UPDATE OR DELETE ON localization.languages
                FOR EACH ROW EXECUTE FUNCTION localization.protect_fallback_language();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP SCHEMA IF EXISTS localization CASCADE;");
    }
}
