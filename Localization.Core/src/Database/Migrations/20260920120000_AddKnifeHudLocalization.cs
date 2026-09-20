using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260920120000_AddKnifeHudLocalization")]
internal sealed class AddKnifeHudLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TEMP TABLE knife_hud_seed(key text, ru text, en text, de text, pl text, parameters jsonb) ON COMMIT DROP;
        INSERT INTO knife_hud_seed VALUES
            ('Menu.Knife.Hud.Title', 'ВЫБОР НОЖА', 'KNIFE SELECTOR', 'MESSERAUSWAHL', 'WYBÓR NOŻA', '[]'::jsonb),
            ('Menu.Knife.Hud.Subtitle', 'Выбери свой стиль. Доминируй в игре.', 'Choose your style. Dominate the game.', 'Wähle deinen Stil. Dominiere das Spiel.', 'Wybierz swój styl. Zdominuj grę.', '[]'::jsonb),
            ('Menu.Knife.Hud.Available', 'ДОСТУПНЫЕ НОЖИ', 'AVAILABLE KNIVES', 'VERFÜGBARE MESSER', 'DOSTĘPNE NOŻE', '[]'::jsonb),
            ('Menu.Knife.Hud.Page', 'СТРАНИЦА {page} / {pages}', 'PAGE {page} / {pages}', 'SEITE {page} / {pages}', 'STRONA {page} / {pages}', '[{"name": "page", "type": "integer", "required": true, "description": "Текущая страница", "example": "1"}, {"name": "pages", "type": "integer", "required": true, "description": "Количество страниц", "example": "3"}]'::jsonb),
            ('Menu.Knife.Hud.Benefits', 'ХАРАКТЕРИСТИКИ', 'BENEFITS', 'EIGENSCHAFTEN', 'WŁAŚCIWOŚCI', '[]'::jsonb),
            ('Menu.Knife.Hud.Description', 'ОПИСАНИЕ', 'DESCRIPTION', 'BESCHREIBUNG', 'OPIS', '[]'::jsonb),
            ('Menu.Knife.Hud.Empty', 'Нет доступных ножей', 'No knives available', 'Keine Messer verfügbar', 'Brak dostępnych noży', '[]'::jsonb),
            ('Menu.Knife.Hud.Equip', 'ЭКИПИРОВАТЬ', 'EQUIP', 'AUSRÜSTEN', 'WYPOSAŻ', '[]'::jsonb),
            ('Menu.Knife.Hud.Equipped', 'ВЫБРАН', 'EQUIPPED', 'AUSGERÜSTET', 'WYPOSAŻONO', '[]'::jsonb),
            ('Menu.Knife.Hud.Select', 'ВЫБРАТЬ', 'SELECT', 'AUSWÄHLEN', 'WYBIERZ', '[]'::jsonb),
            ('Menu.Knife.Hud.Locked', 'НЕДОСТУПЕН', 'LOCKED', 'GESPERRT', 'ZABLOKOWANY', '[]'::jsonb),
            ('Menu.Knife.Hud.CurrentlyEquipped', 'Сейчас экипирован', 'Currently equipped', 'Derzeit ausgerüstet', 'Obecnie wyposażony', '[]'::jsonb),
            ('Menu.Knife.Hud.Ready', 'Готов к экипировке', 'Ready to equip', 'Bereit zum Ausrüsten', 'Gotowy do wyposażenia', '[]'::jsonb),
            ('Menu.Knife.Hud.PermissionRequired', 'Требуется разрешение: {permission}', 'Requires permission: {permission}', 'Berechtigung erforderlich: {permission}', 'Wymagane uprawnienie: {permission}', '[{"name": "permission", "type": "string", "required": true, "description": "Необходимое разрешение", "example": "customknife.vip"}]'::jsonb),
            ('Menu.Knife.Hud.Unavailable', 'Меню ножей недоступно. Закройте другие меню и проверьте ресурсы HUD.', 'Knife menu unavailable. Close other menus and check the HUD resources.', 'Messermenü nicht verfügbar. Schließe andere Menüs und prüfe die HUD-Ressourcen.', 'Menu noży jest niedostępne. Zamknij inne menu i sprawdź zasoby HUD.', '[]'::jsonb),
            ('Menu.Knife.Hud.Stat.Speed', 'Скорость: {value}', 'Speed: {value}', 'Geschwindigkeit: {value}', 'Szybkość: {value}', '[{"name": "value", "type": "string", "required": true, "description": "Значение характеристики ножа", "example": "250"}]'::jsonb),
            ('Menu.Knife.Hud.Stat.Damage', 'Урон: ×{value}', 'Damage: ×{value}', 'Schaden: ×{value}', 'Obrażenia: ×{value}', '[{"name": "value", "type": "string", "required": true, "description": "Значение характеристики ножа", "example": "250"}]'::jsonb),
            ('Menu.Knife.Hud.Stat.Gravity', 'Гравитация: {value}', 'Gravity: {value}', 'Schwerkraft: {value}', 'Grawitacja: {value}', '[{"name": "value", "type": "string", "required": true, "description": "Значение характеристики ножа", "example": "250"}]'::jsonb),
            ('Menu.Knife.Hud.Stat.Knockback', 'Отдача: {value}', 'Knockback: {value}', 'Rückstoß: {value}', 'Odrzut: {value}', '[{"name": "value", "type": "string", "required": true, "description": "Значение характеристики ножа", "example": "250"}]'::jsonb),
            ('Menu.Knife.Hud.Rarity.Common', 'Обычный', 'Common', 'Gewöhnlich', 'Zwykły', '[]'::jsonb),
            ('Menu.Knife.Hud.Rarity.Uncommon', 'Необычный', 'Uncommon', 'Ungewöhnlich', 'Nietypowy', '[]'::jsonb),
            ('Menu.Knife.Hud.Rarity.Rare', 'Редкий', 'Rare', 'Selten', 'Rzadki', '[]'::jsonb),
            ('Menu.Knife.Hud.Rarity.Restricted', 'Запрещённый', 'Restricted', 'Verboten', 'Zakazany', '[]'::jsonb),
            ('Menu.Knife.Hud.Rarity.Classified', 'Засекреченный', 'Classified', 'Vertraulich', 'Poufny', '[]'::jsonb),
            ('Menu.Knife.Hud.Rarity.Elite', 'Элитный', 'Elite', 'Elite', 'Elitarny', '[]'::jsonb),
            ('Menu.Knife.Hud.Rarity.Prototype', 'Прототип', 'Prototype', 'Prototyp', 'Prototyp', '[]'::jsonb),
            ('Menu.Knife.Hud.Rarity.Legendary', 'Легендарный', 'Legendary', 'Legendär', 'Legendarny', '[]'::jsonb);
        INSERT INTO localization.entries(key, description, parameters, is_critical)
        SELECT key, 'Интерфейс выбора ножа Panorama', parameters, FALSE FROM knife_hud_seed
        ON CONFLICT (key) DO NOTHING;
        INSERT INTO localization.translations(entry_id, language_code, text)
        SELECT entry.id, language.code, translated.text
        FROM knife_hud_seed seed
        JOIN localization.entries entry ON entry.key = seed.key
        CROSS JOIN LATERAL (VALUES ('ru', seed.ru), ('en', seed.en), ('de', seed.de), ('pl', seed.pl)) translated(code, text)
        JOIN localization.languages language ON language.code = translated.code
        ON CONFLICT (entry_id, language_code) DO NOTHING;
        DROP TABLE knife_hud_seed;
        UPDATE localization.settings SET configuration_version = configuration_version + 1, updated_at = NOW();
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Сохраняем пользовательские переводы и ссылки на ключи при откате.
    }
}
