using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260906120000_AddHumanClassAndAbilityLocalization")]
internal sealed class AddHumanClassAndAbilityLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.HClass.Selected', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.HClass.Selected'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', '{class} [выбран]'), ('en', '{class} [selected]'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.HClass.SelectionSuccess', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.HClass.SelectionSuccess'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Вы выбрали класс человека: {class}'), ('en', 'Human class selected: {class}'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.HClass.Title', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.HClass.Title'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Классы людей'), ('en', 'Human classes'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.Main.Item.HClass.Title', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.Main.Item.HClass.Title'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Выбрать класс человека'), ('en', 'Select human class'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Blind.Description', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Blind.Description'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Ослепляет противников в радиусе действия'), ('en', 'Blinds enemies within range'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Blind.Name', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Blind.Name'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Ослепление'), ('en', 'Blind'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Catch.Description', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Catch.Description'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Захватывает противника под прицелом и притягивает к владельцу'), ('en', 'Grabs an enemy under the crosshair and pulls them toward the caster'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Catch.Name', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Catch.Name'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Притягивание'), ('en', 'Catch'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Charge.Description', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Charge.Description'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Временно разгоняет владельца до заданной скорости, затем возвращает исходную'), ('en', 'Temporarily accelerates the caster, then restores their speed'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Charge.Name', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Charge.Name'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Рывок'), ('en', 'Charge'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Double.Jump.Description', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Double.Jump.Description'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Позволяет выполнить дополнительный прыжок в воздухе'), ('en', 'Allows an additional jump in the air'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Double.Jump.Name', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Double.Jump.Name'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Двойной прыжок'), ('en', 'Double Jump'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Heal.Description', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Heal.Description'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Лечит другого живого союзника под прицелом, до его максимального HP — человек лечит человека, зомби лечит зомби'), ('en', 'Heals another living ally under the crosshair, up to their maximum health'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Heal.Name', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Heal.Name'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Лечение'), ('en', 'Heal'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Leap.Description', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Leap.Description'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Толкает владельца вперёд и вверх в направлении взгляда'), ('en', 'Propels the caster forward and upward in their view direction'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Leap.Name', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Leap.Name'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Прыжок вперёд'), ('en', 'Leap'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Trap.Description', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Trap.Description'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Ставит ловушку на земле, которая действует на противников'), ('en', 'Places a ground trap affecting enemies'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.Ability.Trap.Name', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.Ability.Trap.Name'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Ловушка'), ('en', 'Trap'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.HClass.Human.Mercenary.Description', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.HClass.Human.Mercenary.Description'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Обычный человек'), ('en', 'Regular human'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.HClass.Human.Mercenary.Name', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.HClass.Human.Mercenary.Name'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Наёмник'), ('en', 'Mercenary'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.HClass.Human.Survivor.Description', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.HClass.Human.Survivor.Description'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Специальный человеческий класс для режима Survivor'), ('en', 'Special human class for Survivor mode'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'ZombiePlague.HClass.Human.Survivor.Name', 'Системный ключ каталога классов', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('ZombiePlague.HClass.Human.Survivor.Name'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Выживший'), ('en', 'Survivor'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW() WHERE id = 1;
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Сохраняем записи и переводы, которые администратор мог изменить
    }
}
