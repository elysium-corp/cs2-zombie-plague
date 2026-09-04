using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Localization.Core.Database.Migrations;

[DbContext(typeof(LocalizationDbContext))]
[Migration("20260904183500_AddShopLocalizationEntries")]
internal sealed class AddShopLocalizationEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TEMP TABLE shop_localization_seed (
                key VARCHAR(191) PRIMARY KEY,
                ru TEXT NOT NULL,
                en TEXT NOT NULL,
                parameters JSONB NOT NULL DEFAULT '[]'::jsonb
            ) ON COMMIT DROP;

            INSERT INTO shop_localization_seed (key, ru, en, parameters) VALUES
                ('Shop.Human.Title', 'Магазин людей', 'Human shop', '[]'::jsonb),
                ('Shop.Zombie.Title', 'Магазин зомби', 'Zombie shop', '[]'::jsonb),
                ('Shop.Menu.Balance', 'Баланс: {balance}$', 'Balance: {balance}$',
                    '[{"name":"balance","type":"integer","required":true,"description":"Текущий баланс игрока","example":"1500"}]'::jsonb),
                ('Shop.Menu.Price', '{price}$', '${price}',
                    '[{"name":"price","type":"integer","required":true,"description":"Цена товара","example":"2500"}]'::jsonb),
                ('Shop.Menu.Ammo', 'Патроны: {amount} за {price}$ (кнопка E)', 'Ammo: {amount} for {price}$ (E key)',
                    '[{"name":"amount","type":"integer","required":true,"description":"Количество патронов","example":"30"},{"name":"price","type":"integer","required":true,"description":"Цена патронов","example":"250"}]'::jsonb),
                ('Shop.Menu.Empty', 'В этом магазине пока нет товаров', 'This shop has no items yet', '[]'::jsonb),
                ('Shop.Menu.Back', 'Назад', 'Back', '[]'::jsonb),
                ('Shop.Commands.Reload.Help', 'Перезагрузить магазин из PostgreSQL или shop.json', 'Reload the shop from PostgreSQL or shop.json', '[]'::jsonb),
                ('Shop.Commands.Status.Help', 'Показать состояние текущего snapshot магазина', 'Show the current shop snapshot status', '[]'::jsonb),
                ('Shop.Admin.Reload.Started', 'Обновление магазина запущено', 'Shop reload started', '[]'::jsonb),
                ('Shop.Admin.Reload.Succeeded', 'Магазин обновлён из {source}. Загружено товаров: {offers}', 'Shop reloaded from {source}. Offers loaded: {offers}',
                    '[{"name":"source","type":"string","required":true,"description":"Источник загруженного snapshot","example":"postgresql"},{"name":"offers","type":"integer","required":true,"description":"Количество загруженных товаров","example":"24"}]'::jsonb),
                ('Shop.Admin.Reload.Failed', 'Не удалось обновить магазин; сохранён предыдущий snapshot', 'Shop reload failed; the previous snapshot remains active', '[]'::jsonb),
                ('Shop.Admin.Status', E'Shop.Core 1.0.0\nИсточник: {source}\nКатегории: {categories}\nТовары: {offers}\nЗагружено: {loaded}', E'Shop.Core 1.0.0\nSource: {source}\nCategories: {categories}\nOffers: {offers}\nLoaded: {loaded}',
                    '[{"name":"source","type":"string","required":true,"description":"Источник текущего snapshot","example":"postgresql"},{"name":"categories","type":"integer","required":true,"description":"Количество категорий","example":"8"},{"name":"offers","type":"integer","required":true,"description":"Количество товаров","example":"24"},{"name":"loaded","type":"string","required":true,"description":"Время загрузки snapshot в ISO 8601","example":"2026-09-04T18:35:00.0000000+00:00"}]'::jsonb),
                ('Shop.Item.Unknown.Name', 'Неизвестный предмет', 'Unknown item', '[]'::jsonb),
                ('Shop.Errors.Unavailable', 'Товар сейчас недоступен', 'This item is currently unavailable', '[]'::jsonb),
                ('Shop.Errors.ProductUnavailable', 'Предмет отсутствует в экипировке сервера', 'The item is missing from the server equipment catalog', '[]'::jsonb),
                ('Shop.Errors.TeamUnavailable', 'Товар недоступен для текущей стороны', 'The item is unavailable for your current side', '[]'::jsonb),
                ('Shop.Errors.AccessDenied', 'Недостаточно прав для покупки', 'You do not have permission to buy this item', '[]'::jsonb),
                ('Shop.Errors.NotEnoughMoney', 'Недостаточно денег', 'Not enough money', '[]'::jsonb),
                ('Shop.Errors.RoundLimit', 'Лимит покупок за раунд исчерпан', 'The per-round purchase limit has been reached', '[]'::jsonb),
                ('Shop.Errors.MapLimit', 'Лимит покупок за карту исчерпан', 'The per-map purchase limit has been reached', '[]'::jsonb),
                ('Shop.Errors.Cooldown', 'Повторная покупка через {seconds} сек.', 'Available again in {seconds} sec.',
                    '[{"name":"seconds","type":"integer","required":true,"description":"Оставшееся время","example":"10"}]'::jsonb),
                ('Shop.Errors.InvalidPlayer', 'Сейчас покупка невозможна', 'You cannot buy anything right now', '[]'::jsonb),
                ('Shop.Errors.Cancelled', 'Покупка отменена', 'Purchase cancelled', '[]'::jsonb),
                ('Shop.Errors.PaymentRejected', 'Не удалось списать деньги', 'Payment was rejected', '[]'::jsonb),
                ('Shop.Errors.GrantRejected', 'Не удалось выдать предмет, деньги возвращены', 'The item could not be granted; your money was refunded', '[]'::jsonb),
                ('Shop.Errors.RefundFailed', 'Не удалось выдать предмет и вернуть деньги. Сообщите администратору', 'The item and refund both failed. Please contact an administrator', '[]'::jsonb),
                ('Shop.Errors.AmmoNotConfigured', 'Для этого оружия покупка патронов не настроена', 'Ammo purchase is not configured for this weapon', '[]'::jsonb),
                ('Shop.Errors.AmmoFull', 'Запас патронов уже заполнен', 'Your ammo reserve is already full', '[]'::jsonb);

            INSERT INTO localization.entries (key, description, is_critical, parameters)
            SELECT seed.key, 'Системный ключ модуля Shop', FALSE, seed.parameters
            FROM shop_localization_seed AS seed
            WHERE NOT EXISTS (
                SELECT 1 FROM localization.entries AS existing
                WHERE lower(existing.key) = lower(seed.key));

            UPDATE localization.entries AS entry
            SET parameters = seed.parameters,
                updated_at = NOW()
            FROM shop_localization_seed AS seed
            WHERE lower(entry.key) = lower(seed.key);

            INSERT INTO localization.translations (entry_id, language_code, text)
            SELECT entry.id, language.code,
                   CASE language.code WHEN 'ru' THEN seed.ru ELSE seed.en END
            FROM shop_localization_seed AS seed
            JOIN localization.entries AS entry ON lower(entry.key) = lower(seed.key)
            JOIN localization.languages AS language ON language.code IN ('ru', 'en')
            ON CONFLICT (entry_id, language_code) DO NOTHING;

            UPDATE localization.settings
            SET configuration_version = configuration_version + 1,
                updated_at = NOW()
            WHERE id = 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Администратор мог изменить переводы после миграции; пользовательские данные не удаляем.
    }
}
