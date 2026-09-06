using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260906160000_AddAbilityHudSettingsLocalization")]
internal sealed class AddAbilityHudSettingsLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Title', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Title'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Панель способностей'), ('en', 'Ability HUD'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Scale', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Scale'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Размер'), ('en', 'Size'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Position', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Position'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Положение'), ('en', 'Position'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.PreviewHint', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.PreviewHint'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Предпросмотр виден у живого игрока с назначенными способностями'), ('en', 'Preview is visible while alive with assigned abilities'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Reset', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Reset'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Сбросить расположение и размер'), ('en', 'Reset position and size'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Done', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Done'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Готово'), ('en', 'Done'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Disabled', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Disabled'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Панель способностей отключена на сервере'), ('en', 'Ability HUD is disabled on this server'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Unavailable', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Unavailable'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Настройки игрока ещё загружаются — попробуйте чуть позже'), ('en', 'Player settings are still loading; please try again shortly'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Position.Top.Left', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Position.Top.Left'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Сверху слева'), ('en', 'Top left'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Position.Top.Center', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Position.Top.Center'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Сверху по центру'), ('en', 'Top center'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Position.Top.Right', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Position.Top.Right'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Сверху справа'), ('en', 'Top right'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Position.Middle.Left', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Position.Middle.Left'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Посередине слева'), ('en', 'Middle left'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Position.Middle.Center', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Position.Middle.Center'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'В центре экрана'), ('en', 'Screen center'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Position.Middle.Right', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Position.Middle.Right'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Посередине справа'), ('en', 'Middle right'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Position.Bottom.Left', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Position.Bottom.Left'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Снизу слева'), ('en', 'Bottom left'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Position.Bottom.Center', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Position.Bottom.Center'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Снизу по центру'), ('en', 'Bottom center'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;

        WITH inserted AS (
            INSERT INTO localization.entries(key, description, is_critical)
            SELECT 'Menu.AbilityHud.Position.Bottom.Right', 'Настройки панели способностей', FALSE
            WHERE NOT EXISTS (SELECT 1 FROM localization.entries WHERE lower(key) = lower('Menu.AbilityHud.Position.Bottom.Right'))
            ON CONFLICT (key) DO NOTHING RETURNING id
        ), seeds(language_code, text) AS (VALUES ('ru', 'Снизу справа'), ('en', 'Bottom right'))
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT inserted.id, language.code, seeds.text FROM inserted CROSS JOIN seeds
        JOIN localization.languages language ON language.code = seeds.language_code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Переводы могли быть отредактированы на сайте, поэтому откат их сохраняет
    }
}
