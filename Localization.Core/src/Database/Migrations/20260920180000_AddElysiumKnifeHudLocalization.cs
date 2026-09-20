using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260920180000_AddElysiumKnifeHudLocalization")]
internal sealed class AddElysiumKnifeHudLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TEMP TABLE elysium_knife_hud_seed(key text, ru text, en text, de text, pl text, parameters jsonb) ON COMMIT DROP;
        INSERT INTO elysium_knife_hud_seed VALUES
            ('Menu.Knife.Hud.ElysiumSubtitle', 'Выбор ножа', 'Knife selector', 'Messerauswahl', 'Wybór noża', '[]'::jsonb),
            ('Menu.Knife.Hud.StatsTitle', 'ХАРАКТЕРИСТИКИ', 'STATS', 'EIGENSCHAFTEN', 'STATYSTYKI', '[]'::jsonb),
            ('Menu.Knife.Hud.ComparisonHint', 'Текущий нож → выбранный', 'Current knife → preview', 'Aktuelles Messer → Vorschau', 'Obecny nóż → podgląd', '[]'::jsonb),
            ('Menu.Knife.Hud.PreviewSelected', 'ВЫБРАН', 'SELECTED', 'AUSGEWÄHLT', 'WYBRANO', '[]'::jsonb),
            ('Menu.Knife.Hud.EquippedState', 'ЭКИПИРОВАН', 'EQUIPPED', 'AUSGERÜSTET', 'WYPOSAŻONO', '[]'::jsonb),
            ('Menu.Knife.Hud.Saved', 'СОХРАНЁН', 'SAVED', 'GESPEICHERT', 'ZAPISANO', '[]'::jsonb),
            ('Menu.Knife.Hud.Save', 'СОХРАНИТЬ', 'SAVE', 'SPEICHERN', 'ZAPISZ', '[]'::jsonb),
            ('Menu.Knife.Hud.EquippedKnife', 'Сейчас экипирован: {knife}', 'Currently equipped: {knife}', 'Derzeit ausgerüstet: {knife}', 'Obecnie wyposażony: {knife}', '[{"name": "knife", "type": "string", "required": true, "description": "Название сохранённого человеческого ножа", "example": "Karambit"}]'::jsonb),
            ('Menu.Knife.Hud.PendingHuman', 'Для человека: {knife} · Применится при переходе за людей', 'For humans: {knife} · Applies when you become human', 'Für Menschen: {knife} · Wird beim Wechsel zum Menschen ausgerüstet', 'Dla człowieka: {knife} · Zostanie użyty po przejściu do ludzi', '[{"name": "knife", "type": "string", "required": true, "description": "Название сохранённого человеческого ножа", "example": "Karambit"}]'::jsonb),
            ('Menu.Knife.Hud.PendingSpawn', 'Сохранён: {knife} · Применится при возрождении', 'Saved: {knife} · Applies on respawn', 'Gespeichert: {knife} · Wird beim Wiedereinstieg ausgerüstet', 'Zapisano: {knife} · Zostanie użyty po odrodzeniu', '[{"name": "knife", "type": "string", "required": true, "description": "Название сохранённого человеческого ножа", "example": "Karambit"}]'::jsonb),
            ('Menu.Knife.Hud.ScaleTitle', 'МАСШТАБ МЕНЮ', 'MENU SCALE', 'MENÜSKALIERUNG', 'SKALA MENU', '[]'::jsonb),
            ('Menu.Knife.Hud.StatLabel.Speed', 'Скорость', 'Speed', 'Geschwindigkeit', 'Szybkość', '[]'::jsonb),
            ('Menu.Knife.Hud.StatLabel.Damage', 'Урон', 'Damage', 'Schaden', 'Obrażenia', '[]'::jsonb),
            ('Menu.Knife.Hud.StatLabel.Gravity', 'Гравитация', 'Gravity', 'Schwerkraft', 'Grawitacja', '[]'::jsonb),
            ('Menu.Knife.Hud.StatLabel.Knockback', 'Отбрасывание', 'Knockback', 'Rückstoß', 'Odrzut', '[]'::jsonb);
        INSERT INTO localization.entries(key, description, parameters, is_critical)
        SELECT key, 'Интерфейс выбора ножа Panorama', parameters, FALSE FROM elysium_knife_hud_seed
        ON CONFLICT (key) DO NOTHING;
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT entry.id, language.code, translated.text
        FROM elysium_knife_hud_seed seed
        JOIN localization.entries entry ON entry.key = seed.key
        CROSS JOIN LATERAL (VALUES ('ru', seed.ru), ('en', seed.en), ('de', seed.de), ('pl', seed.pl)) translated(code, text)
        JOIN localization.languages language ON language.code = translated.code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        DROP TABLE elysium_knife_hud_seed;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Сохраняем пользовательские переводы и ссылки на ключи при откате.
    }
}
